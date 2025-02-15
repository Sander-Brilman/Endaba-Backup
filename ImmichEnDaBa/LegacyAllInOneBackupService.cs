using System;
using System.Diagnostics;
using ImmichEnDaBa.DataStores;
using ImmichEnDaBa.Settings;

namespace ImmichEnDaBa;

public sealed class AllInOneBackupService(AppSettings appSettings, IDataStore dataStore)
{
    private readonly AppSettings appSettings = appSettings;
    private readonly IDataStore dataStore = dataStore;
    private readonly PathResolver pathResolver = new();
    private readonly ZippingService zippingService = new();

    public async Task TriggerBackup(CancellationToken cancellationToken) 
    {
        List<string> filesToProcess = appSettings.BackupLocationPatterns
            .SelectMany(pathResolver.GetFilesFromPath)
            .ToList();

        string basePath = appSettings.BasePath;

        foreach (var file in filesToProcess)
        {
            string relativeFilePath = file[basePath.Length..] + ".zip";

            if (await dataStore.DoesFileExist(relativeFilePath, cancellationToken)) {
                continue;
            }

            string tempZipFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName() + ".zip");
            
            zippingService.ZipFile(file, tempZipFile, appSettings.EncryptionKey); 

            await dataStore.UploadFile(relativeFilePath, tempZipFile, cancellationToken);

            SafeFileDelete.Delete(tempZipFile);
        }
    }
}
