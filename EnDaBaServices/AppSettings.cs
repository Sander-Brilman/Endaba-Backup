using System;

namespace EnDaBaBackup;

/// <summary>
/// Static app settings (such as file extensions). Not meant to be changed at runtime
/// </summary>
public static class AppSettings
{
    public static readonly string ZipFileExtension = ".zip";
    public static readonly string HashFileExtension = ".hash.txt";
    public static readonly string TempFolder = Path.GetTempPath();
}
