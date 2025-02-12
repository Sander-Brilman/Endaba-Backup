
using ImmichEnDaBa.DataStores.FTP;
using ImmichEnDaBa.Settings;

SettingsService settingsService = new();

if (settingsService.IsSettingsFilePresent() is false) 
{
    Console.WriteLine($"No settings file present, a example one will be generated at " + SettingsService.SettingFilePath);
    Console.WriteLine("");
    Console.WriteLine("Fill in the settings and then rerun the program");
    settingsService.GenerateExampleSettingsFile();
}

AppSettings settings = settingsService.GetSettings()!;


FTPDataStore dataStore = new FTPDataStore();

Console.WriteLine("Hello, World!");
