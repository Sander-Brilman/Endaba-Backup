using System;

namespace EnDaBaServices;

public sealed class LoggingService(string logFilePath)
{
    private readonly string logFilePath = logFilePath;

    private async Task LogString(string logType, string message)
    {
        await File.AppendAllTextAsync(logFilePath, $"\n{logType,-7} [{DateTime.Now:MM/dd/yy H:mm:ss}] {message}");
    }

    public Task LogInfo(string info)
    {
        return LogString("[Info]", info);
    }

    public Task LogError(string error)
    {
        return LogString("[Error]", error);
    }
}
