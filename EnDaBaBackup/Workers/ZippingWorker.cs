using EnDaBaServices;
using EnDaBaServices.Settings;

namespace EnDaBaBackup.Workers;

public sealed class ZippingWorker(
    JobDispatcher<ZippingJob> sourceJobDispatcher,
    JobDispatcher<UploadJob> uploadJobDispatcher,
    BackupSettings settings
) : JobWorker<ZippingJob>(sourceJobDispatcher)
{
    private readonly ZippingService zippingService = new();
    private readonly JobDispatcher<UploadJob> uploadJobDispatcher = uploadJobDispatcher;
    private readonly BackupSettings settings = settings;
    
    private static readonly string tmpFolder = Path.GetTempPath();


    public override async Task ProcessJob(ZippingJob job, CancellationToken cancellationToken)
    {
        string remoteFilePath = job.SourceFilePath[settings.BasePath.Length..];

        string tempZipFileLocation = Path.Combine(tmpFolder, Path.GetTempFileName() + ".zip");
        string tempHashFileLocation = Path.Combine(tmpFolder, Path.GetTempFileName() + ".hash.txt");

        await File.WriteAllTextAsync(tempHashFileLocation, job.Hash, cancellationToken);
        zippingService.ZipFile(job.SourceFilePath, tempZipFileLocation, settings.EncryptionKey);

        string remoteZipFilePath = remoteFilePath + ".zip";
        string remoteHashFilePath =remoteFilePath + ".hash.txt";

        uploadJobDispatcher.AddJobToQueue(new UploadJob(tempZipFileLocation, remoteZipFilePath, tempHashFileLocation, remoteHashFilePath));
    }   
}

public sealed record ZippingJob(string SourceFilePath, string Hash) : JobBase;