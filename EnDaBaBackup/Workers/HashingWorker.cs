using System;
using EnDaBaServices;

namespace EnDaBaBackup.Workers;

public sealed class HashingWorker(
    JobDispatcher<FileCheckJob> fileCheckJobDispatcher
) : JobWorker<HashingJob>
{
    private readonly JobDispatcher<FileCheckJob> fileCheckJobDispatcher = fileCheckJobDispatcher;

    private readonly HashService hashService = new();

    public override async Task ProcessJob(HashingJob job, CancellationToken cancellationToken)
    {
        string hash = await hashService.CalculateHashFromFile(job.SourceFile, cancellationToken); 

        await fileCheckJobDispatcher.AddJobToQueueAsync(new FileCheckJob(job.SourceFile, hash));
    }
}

public sealed record HashingJob(string SourceFile) : JobBase;