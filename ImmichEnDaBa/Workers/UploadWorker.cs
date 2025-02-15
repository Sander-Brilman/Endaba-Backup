using System;
using ImmichEnDaBa.DataStores;

namespace ImmichEnDaBa.Workers;

public sealed class UploadWorker(
    JobDispatcher<UploadJob> sourceJobDispenser,
    IDataStore dataStore
) : JobWorker<UploadJob>(sourceJobDispenser)
{
    private readonly IDataStore dataStore = dataStore;

    public override async Task ProcessJob(UploadJob job, CancellationToken cancellationToken)
    {
        await dataStore.UploadFile(job.RemoteFilePath, job.LocalFilePath, cancellationToken);

        SafeFileDelete.Delete(job.LocalFilePath);
    }
}

public sealed record UploadJob(string LocalFilePath, string RemoteFilePath);