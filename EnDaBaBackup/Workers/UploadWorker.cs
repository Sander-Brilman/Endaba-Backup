using System;
using EnDaBaServices;
using EnDaBaServices.DataStores;

namespace EnDaBaBackup.Workers;

public sealed class UploadWorker(
    JobDispatcher<UploadJob> sourceJobDispatcher,
    IDataStore dataStore
) : JobWorker<UploadJob>(sourceJobDispatcher)
{
    private readonly IDataStore dataStore = dataStore;

    public override async Task ProcessJob(UploadJob job, CancellationToken cancellationToken)
    {
        await dataStore.UploadFile(job.LocalFilePath, job.RemoteFilePath, cancellationToken);
        await dataStore.UploadFile(job.LocalHashFilePath, job.RemoteHashFilePath, cancellationToken);

        SafeFileDelete.Delete(job.LocalFilePath);
    }
}

public sealed record UploadJob(string LocalFilePath, string RemoteFilePath, string LocalHashFilePath, string RemoteHashFilePath) : JobBase;