using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Serilog;
using Serilog.Events;
using WhistleblowerNews.Application.Abstractions;
using WhistleblowerNews.Application.Auditing;
using WhistleblowerNews.Application.Authorization;
using WhistleblowerNews.Application.Articles;
using WhistleblowerNews.Application.Comments;
using WhistleblowerNews.Application.Reports;
using WhistleblowerNews.Domain;
using WhistleblowerNews.Infrastructure;
using WhistleblowerNews.Web.Infrastructure.Email;
using WhistleblowerNews.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

builder.Host.UseSerilog((context, services, config) =>
{
    var http = services.GetService<IHttpContextAccessor>() ?? new HttpContextAccessor();

    config.MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ApplicationName", "WhistleblowerNews")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .Enrich.With(new UserContextEnricher(http))
        .Enrich.With(new SensitiveDataEnricher())
        .WriteTo.Console();
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing connection string 'DefaultConnection'. " +
        "Configure it in appsettings.json under ConnectionStrings:DefaultConnection.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
builder.Services.AddControllersWithViews();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("reporter-token-policy", context =>
    {
        var config = context.RequestServices.GetRequiredService<IConfiguration>();
        var permitLimit = config.GetValue("RateLimiting:ReporterToken:PermitLimit", 5);
        var windowSeconds = config.GetValue("RateLimiting:ReporterToken:WindowSeconds", 10);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    options.AddPolicy("report-submit-policy", context =>
    {
        var config = context.RequestServices.GetRequiredService<IConfiguration>();
        var permitLimit = config.GetValue("RateLimiting:ReportSubmit:PermitLimit", 3);
        var windowSeconds = config.GetValue("RateLimiting:ReportSubmit:WindowSeconds", 60);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    options.AddPolicy("auth-login-policy", context =>
    {
        var config = context.RequestServices.GetRequiredService<IConfiguration>();
        var permitLimit = config.GetValue("RateLimiting:AuthLogin:PermitLimit", 10);
        var windowSeconds = config.GetValue("RateLimiting:AuthLogin:WindowSeconds", 60);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    options.AddPolicy("auth-register-policy", context =>
    {
        var config = context.RequestServices.GetRequiredService<IConfiguration>();
        var permitLimit = config.GetValue("RateLimiting:AuthRegister:PermitLimit", 5);
        var windowSeconds = config.GetValue("RateLimiting:AuthRegister:WindowSeconds", 60);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.Cookie.Name = "WhistleblowerNews.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;

    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.IsWriter, policy =>
        policy.RequireRole(UserRole.Writer.ToString()));

    options.AddPolicy(AuthorizationPolicies.IsEditor, policy =>
        policy.RequireRole(UserRole.Editor.ToString()));

    options.AddPolicy(AuthorizationPolicies.IsSubscriber, policy =>
        policy.RequireRole(UserRole.Subscriber.ToString()));

    options.AddPolicy(AuthorizationPolicies.IsInvestigator, policy =>
        policy.RequireRole(UserRole.Investigator.ToString(), UserRole.Editor.ToString()));

    options.AddPolicy(AuthorizationPolicies.ArticleOwnerOrEditor, policy =>
        policy.Requirements.Add(new ArticleOwnerOrEditorRequirement()));

    options.AddPolicy(AuthorizationPolicies.CommentOwnerOrEditor, policy =>
        policy.Requirements.Add(new CommentOwnerOrEditorRequirement()));

    options.AddPolicy(AuthorizationPolicies.WriterOwnsArticle, policy =>
        policy.Requirements.Add(new WriterOwnsArticleRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, ArticleOwnerOrEditorHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, CommentOwnerOrEditorHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, WriterOwnsArticleHandler>();

builder.Services.AddScoped<ArticleService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.Configure<FileEmailSenderOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddSingleton<IEmailSender, FileEmailSender>();

builder.Services.AddHealthChecks()
    .AddCheck<DbHealthCheck>("db");

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
        diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value ?? string.Empty);
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
    };
});

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalException");

        if (exception is not null)
            logger.LogError(exception, "Unhandled exception");

        if (context.Response.HasStarted)
            return;

        var isApi = context.Request.Path.StartsWithSegments("/api");

        if (isApi)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            };
            problem.Extensions["correlationId"] = context.TraceIdentifier;

            await context.Response.WriteAsJsonAsync(problem);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.Redirect("/Home/Error");
    });
});

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    branch => branch.Use(async (context, next) =>
    {
        if (!context.Request.IsHttps && !app.Environment.IsEnvironment("Testing"))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        await next();
    }));

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseHttpsRedirection());

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "form-action 'self'; " +
        "style-src 'self'; " +
        "script-src 'self'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "frame-ancestors 'none'";

    await next();
});

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api") &&
               (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)),
    branch => branch.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}"));

app.UseStaticFiles();
app.UseRouting();

app.UseMiddleware<SecurityEventLoggingMiddleware>();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    await DatabaseSeeder.SeedAsync(db, userManager, roleManager);
}

app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
