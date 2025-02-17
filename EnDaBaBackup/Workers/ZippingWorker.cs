using EnDaBaServices;
using EnDaBaServices.Settings;

namespace EnDaBaBackup.Workers;

public sealed class ZippingWorker(
    JobDispatcher<UploadJob> uploadJobDispatcher,
    BackupSettings settings
) : JobWorker<ZippingJob>
{
    private readonly ZippingService zippingService = new();
    private readonly JobDispatcher<UploadJob> uploadJobDispatcher = uploadJobDispatcher;
    private readonly BackupSettings settings = settings;
    
    public override async Task ProcessJob(ZippingJob job, CancellationToken cancellationToken)
    {
        string remoteFilePath = job.SourceFilePath[settings.GetBasePath().Length..];

        string tempZipFileLocation = Path.Combine(AppSettings.TempFolder, Path.GetTempFileName() + AppSettings.ZipFileExtension);
        string tempHashFileLocation = Path.Combine(AppSettings.TempFolder, Path.GetTempFileName() + AppSettings.HashFileExtension);

        await File.WriteAllTextAsync(tempHashFileLocation, job.Hash, cancellationToken);
        zippingService.ZipFile(job.SourceFilePath, tempZipFileLocation, settings.EncryptionKey);

        string remoteZipFilePath = remoteFilePath + AppSettings.ZipFileExtension;
        string remoteHashFilePath =remoteFilePath + AppSettings.HashFileExtension;

        await uploadJobDispatcher.AddJobToQueueAsync(new UploadJob(tempZipFileLocation, remoteZipFilePath, tempHashFileLocation, remoteHashFilePath));
    }   
}

public sealed record ZippingJob(string SourceFilePath, string Hash) : JobBase;