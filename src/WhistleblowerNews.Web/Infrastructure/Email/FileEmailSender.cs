using System.Text;
using Microsoft.Extensions.Options;

namespace WhistleblowerNews.Web.Infrastructure.Email;

public sealed class FileEmailSenderOptions
{
    public string FilePath { get; set; } = "logs/dev-emails.log";
    public string FromAddress { get; set; } = "no-reply@whistleblowernews.local";
}

public sealed class FileEmailSender : IEmailSender
{
    private readonly FileEmailSenderOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileEmailSender> _logger;

    public FileEmailSender(
        IOptions<FileEmailSenderOptions> options,
        IWebHostEnvironment environment,
        ILogger<FileEmailSender> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var path = _options.FilePath;
        if (!Path.IsPathRooted(path))
            path = Path.Combine(_environment.ContentRootPath, path);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var message = new StringBuilder()
            .AppendLine("=== Dev Email ===")
            .AppendLine($"UTC: {DateTime.UtcNow:u}")
            .AppendLine($"From: {_options.FromAddress}")
            .AppendLine($"To: {to}")
            .AppendLine($"Subject: {subject}")
            .AppendLine()
            .AppendLine(htmlBody)
            .AppendLine()
            .AppendLine("=== End ===")
            .AppendLine()
            .ToString();

        await File.AppendAllTextAsync(path, message, Encoding.UTF8, ct);
        _logger.LogInformation("Dev email written to {EmailPath}", path);
    }
}
