
using ImmichEnDaBa;
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

string fileLocal = "/home/sander/Documents/test-file.txt";
string fileDesLocal = "/home/sander/Documents/thing.zip";
// string fileDesLocal2 = "/home/sander/";

ZippingService zippingService = new();

zippingService.ZipFile(fileLocal, fileDesLocal, settings.EncryptionKey);

// zippingService.UnzipFile(fileDesLocal, fileDesLocal2, settings.EncryptionKey);

FTPDataStore dataStore = await FTPDataStore.GenerateNewFromCredentials(
    settings.FtpHost,
    settings.FtpPort,
    settings.FtpUsername,
    settings.FtpPassword
);

await dataStore.UploadFile("/text-files/zipped-text/test-file.zip", fileDesLocal);

Console.WriteLine("Hello, World!");
