using System;
using EnDaBaServices;
using EnDaBaServices.DataStores;
using EnDaBaServices.DataStores.FTP;
using EnDaBaServices.Settings;

namespace EnDaBaBackup.Workers;

public sealed class CheckingWorker(
    JobDispatcher<ZippingJob> zippingJobDispatcher,
    IDataStore dataStore,
    BackupSettings settings
) : JobWorker<FileCheckJob>
{
    private readonly JobDispatcher<ZippingJob> zippingJobDispatcher = zippingJobDispatcher;
    private readonly IDataStore dataStore = dataStore;
    private readonly BackupSettings settings = settings;

    public override async Task ProcessJob(FileCheckJob job, CancellationToken cancellationToken)
    {
        string remoteHashFilePath = job.FilePath[settings.GetBasePath().Length..] + AppSettings.HashFileExtension;

        string? remoteHash = await dataStore.GetFileContentsAsString(remoteHashFilePath, cancellationToken);


        // hash is present and still the same with the current version
        if (remoteHash is not null && remoteHash == job.LocalHash) {
            return;
        }


        // If no hash is present (which is unlikely) or the hash isn't equal we need to re-upload both files.
        // 
        // in scenario 1 the file likely hasn't been uploaded yet or had its hash deleted. If it had
        // its hash deleted we cannot know the version of the remote file without downloading it since not all (ftp) servers support sending a checksum.
        // in ftp servers uploading is often faster then downloading so just re-upload them both.
        // 
        // in scenario 2 the file is a version behind. we need to update both the file and the hash
        //
        // So in both cases just re-upload both files. 
       
        await zippingJobDispatcher.AddJobToQueueAsync(new ZippingJob(job.FilePath, job.LocalHash));
    }

    public static async Task<CheckingWorker> CreateNew(WorkerManager<ZippingJob> zippingManager, FTPSettings ftpSettings, BackupSettings backupSettings) 
    {
        FTPDataStore dataStore = await FTPDataStore.GenerateNewFromSettings(ftpSettings);
        return new CheckingWorker(zippingManager.Dispatcher, dataStore, backupSettings);    
    }
}


public sealed record FileCheckJob(string FilePath, string LocalHash) : JobBase;