using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using whistleblowerNews.Authorization;
using whistleblowerNews.Domain;
using whistleblowerNews.Infrastructure;
using whistleblowerNews.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration sanity checks (fail fast) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing connection string 'DefaultConnection'. " +
        "Configure it in appsettings.json under ConnectionStrings:DefaultConnection.");
}

var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey))
    throw new InvalidOperationException("Missing config Jwt:SigningKey in appsettings.json.");

if (signingKey.Length < 32)
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters long.");

// --- Services (DI container) ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// JWT token creation service
builder.Services.AddSingleton<JwtTokenService>();

// Authentication (who are you?)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
        };
    });

// Authorization (are you allowed?)
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

var app = builder.Build();
Console.WriteLine($"🌍 ASPNETCORE_ENVIRONMENT = {app.Environment.EnvironmentName}");

// --- Development-only tooling ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DatabaseSeeder.SeedAsync(db);
}

// --- Middleware pipeline (order matters!) ---
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Required for WebApplicationFactory in tests
public partial class Program { }
