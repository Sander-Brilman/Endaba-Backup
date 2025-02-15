using System;
using System.Text.Json;

namespace ImmichEnDaBa.Settings;

public sealed class SettingsService<TSettings>(string settingsFileName)
    where TSettings : class
{
    public readonly string SettingFilePath = Path.Combine(Directory.GetCurrentDirectory(), settingsFileName);

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

    public void WriteSettingsToFile(TSettings settings)
    {
        string Json = JsonSerializer.Serialize(settings, niceLookingJsonOptions);

        File.CreateText(SettingFilePath);
        File.WriteAllText(SettingFilePath, Json);
    }

    public TSettings? GetSettings() 
    {
        string json = File.ReadAllText(SettingFilePath);
        return JsonSerializer.Deserialize<TSettings>(json, compatibilityRead);
    }
}
