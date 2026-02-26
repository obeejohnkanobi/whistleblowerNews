using Serilog.Core;
using Serilog.Events;

namespace WhistleblowerNews.Web.Infrastructure;

public sealed class SensitiveDataEnricher : ILogEventEnricher
{
    private static readonly string[] PropertyNames =
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "ReporterToken",
        "X-Reporter-Token",
        "Password"
    };

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var name in PropertyNames)
            logEvent.RemovePropertyIfPresent(name);
    }
}
