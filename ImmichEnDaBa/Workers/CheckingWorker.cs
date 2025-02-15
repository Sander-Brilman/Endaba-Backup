using System;
using ImmichEnDaBa.DataStores;
using ImmichEnDaBa.Settings;

namespace ImmichEnDaBa.Workers;

public sealed class CheckingWorker(
    JobDispatcher<FileCheckJob> sourceJobDispenser, 
    JobDispatcher<ZippingJob> zippingJobDispenser,
    IDataStore dataStore,
    AppSettings settings
) : JobWorker<FileCheckJob>(sourceJobDispenser)
{
    private readonly JobDispatcher<ZippingJob> zippingJobDispenser = zippingJobDispenser;
    private readonly IDataStore dataStore = dataStore;
    private readonly AppSettings settings = settings;

    public override async Task ProcessJob(FileCheckJob job, CancellationToken cancellationToken)
    {
        string pathFromBase = job.FilePath[settings.BasePath.Length..];

        if (await dataStore.DoesFileExist(pathFromBase, cancellationToken)) {
            return;
        }


        zippingJobDispenser.AddJobToQueue(new ZippingJob(job.FilePath));
    }
}


public sealed record FileCheckJob(string FilePath);