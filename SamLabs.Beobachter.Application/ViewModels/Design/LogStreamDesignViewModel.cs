using System;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.ViewModels.Design;

public sealed class LogStreamDesignViewModel : LogStreamViewModel
{
    public LogStreamDesignViewModel()
    {
        LevelColumnWidth = 40;
        TimestampColumnWidth = 170;
        LoggerColumnWidth = 210;

        DateTimeOffset now = DateTimeOffset.Now;
        Append(
            now.AddSeconds(-26),
            LogLevel.Error,
            "api-gateway",
            "Database connection failed for tenant alpha.");
        Append(
            now.AddSeconds(-21),
            LogLevel.Warn,
            "worker-pool",
            "High memory usage detected (87%).");
        Append(
            now.AddSeconds(-17),
            LogLevel.Info,
            "auth-service",
            "User authentication successful.");
        Append(
            now.AddSeconds(-13),
            LogLevel.Debug,
            "profile-service",
            "Cache hit for profile lookup.");
        Append(
            now.AddSeconds(-8),
            LogLevel.Trace,
            "message-bus",
            "Heartbeat frame accepted from node-02.");
        Append(
            now.AddSeconds(-4),
            LogLevel.Fatal,
            "payment-service",
            "Circuit breaker open after repeated gateway failures.");
    }

    private void Append(DateTimeOffset timestamp, LogLevel level, string logger, string message)
    {
        VisibleEntries.Add(new LogEntry
        {
            Timestamp = timestamp,
            Level = level,
            ReceiverId = "design",
            LoggerName = logger,
            RootLoggerName = logger,
            Message = message
        });
    }
}
