using System;
using EnDaBaServices;
using EnDaBaServices.Workers;

namespace EnDaBaBackup.Workers;

public sealed class HashingWorker(
    JobDispatcher<HashingJob> sourceJobDispatcher,
    JobDispatcher<FileCheckJob> fileCheckJobDispatcher
    
) : JobWorker<HashingJob>(sourceJobDispatcher)
{
    private readonly JobDispatcher<FileCheckJob> fileCheckJobDispatcher = fileCheckJobDispatcher;

    private readonly HashService hashService = new();

    // private static readonly string hashLocations = Path.GetTempPath();

    public override async Task ProcessJob(HashingJob job, CancellationToken cancellationToken)
    {
        // string tempLocation = Path.Combine(hashLocations, Path.GetTempFileName() + ".hash.txt");

        string hash = await hashService.CalculateHashFromFile(job.SourceFile, cancellationToken); 

        fileCheckJobDispatcher.AddJobToQueue(new FileCheckJob(job.SourceFile, hash));
    }
}

public sealed record HashingJob(string SourceFile) : JobBase;