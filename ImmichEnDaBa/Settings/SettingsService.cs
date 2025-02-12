using System;
using System.Text.Json;

namespace ImmichEnDaBa.Settings;

public class SettingsService
{
    public static readonly string SettingFilePath = Path.GetFullPath("./AppSettings.json");

    private readonly JsonSerializerOptions niceLookingJsonOptions = new() { 
        WriteIndented = true 
    };

    private readonly JsonSerializerOptions compatibilityRead = new() {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
    };

    public bool IsSettingsFilePresent() {
        return File.Exists(SettingFilePath);
    }

    public void GenerateExampleSettingsFile()
    {
        AppSettings settings = new AppSettings(
            [
                "/home/john/immich-app/library/backups/",
                "/home/john/immich-app/library/upload/*/*/*/",
                "/home/john/immich-app/library/profile/*/",
                "/home/john/immich-app/library/library/",
            ],
            "ftp.example.com",
            21,
            "johnftp",
            "my_secret_ftp_password",
            "my_encryption_key"
        );

        string Json = JsonSerializer.Serialize(settings, niceLookingJsonOptions);

        File.CreateText(SettingFilePath);
        File.WriteAllText(SettingFilePath, Json);
    }

    public AppSettings? GetSettings() 
    {
        string json = File.ReadAllText(SettingFilePath);
        return JsonSerializer.Deserialize<AppSettings>(json, compatibilityRead);
    }


}
