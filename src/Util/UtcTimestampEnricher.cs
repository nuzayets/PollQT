using System;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace PollQT.Util
{
    internal class UtcTimestampEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory pf) => logEvent.AddPropertyIfAbsent(pf.CreateProperty("UtcTimestamp", logEvent.Timestamp.UtcDateTime));
    }

    internal static class PollQTLoggerConfigurationExtensions
    {
        public static LoggerConfiguration WithUtcTimestamp(this LoggerEnrichmentConfiguration enrichmentConfiguration) {
            _ = enrichmentConfiguration ?? throw new ArgumentNullException(nameof(enrichmentConfiguration));
            return enrichmentConfiguration.With<UtcTimestampEnricher>();
        }
    }
}

