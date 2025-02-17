using System;
using EnDaBaServices;
using EnDaBaServices.DataStores;
using EnDaBaServices.DataStores.FTP;

namespace EnDaBaBackup.Workers;

public sealed class UploadWorker(
    IDataStore dataStore
) : JobWorker<UploadJob>
{
    private readonly IDataStore dataStore = dataStore;

    public override async Task ProcessJob(UploadJob job, CancellationToken cancellationToken)
    {
        await dataStore.UploadFile(job.LocalFilePath, job.RemoteFilePath, cancellationToken);
        await dataStore.UploadFile(job.LocalHashFilePath, job.RemoteHashFilePath, cancellationToken);

        SafeFileDelete.Delete(job.LocalFilePath);
        SafeFileDelete.Delete(job.LocalHashFilePath);
    }

    public static async Task<UploadWorker> CreateNew(FTPSettings ftpSettings) 
    {
        FTPDataStore dataStore = await FTPDataStore.GenerateNewFromSettings(ftpSettings);
        return new UploadWorker(dataStore);
    }
}

public sealed record UploadJob(string LocalFilePath, string RemoteFilePath, string LocalHashFilePath, string RemoteHashFilePath) : JobBase;