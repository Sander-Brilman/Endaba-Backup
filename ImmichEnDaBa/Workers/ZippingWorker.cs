using ImmichEnDaBa.Settings;

namespace ImmichEnDaBa.Workers;

public sealed class ZippingWorker(
    JobDispatcher<ZippingJob> sourceJobDispenser,
    JobDispatcher<UploadJob> uploadJobDispenser,
    AppSettings settings
) : JobWorker<ZippingJob>(sourceJobDispenser)
{
    private readonly ZippingService zippingService = new();
    private readonly JobDispatcher<UploadJob> uploadJobDispenser = uploadJobDispenser;
    private readonly AppSettings settings = settings;
    
    private static readonly string zipFileLocations = Path.GetTempPath();


    public override Task ProcessJob(ZippingJob job, CancellationToken cancellationToken)
    {
        string tempLocation = Path.Combine(zipFileLocations, Path.GetTempFileName() + ".zip");

        zippingService.ZipFile(job.SourceFilePath, tempLocation);

        string remotePath = job.SourceFilePath[settings.BasePath.Length..] + ".zip";

        uploadJobDispenser.AddJobToQueue(new UploadJob(tempLocation, remotePath));

        return Task.CompletedTask;
    }   
}

public sealed record ZippingJob(string SourceFilePath);