using System.Diagnostics;
using EnDaBaServices;
using EnDaBaServices.DataStores.FTP;
using EnDaBaServices.Settings;
using EnDaBaServices.Workers;
using EnDaBaBackup.Workers;

SettingsService<BackupSettings> generalSettingsService = new("general-settings.json");
SettingsService<FTPSettings> FTPSettingsService = new("ftp-settings.json");

if (generalSettingsService.IsSettingsFilePresent() is false) 
{
    Console.WriteLine($"No general settings file present, a example one will be generated at " + generalSettingsService.SettingFilePath);
    Console.WriteLine("");
    Console.WriteLine("Fill in the settings and then rerun the program");

    generalSettingsService.WriteSettingsToFile(new BackupSettings(
        [
            "/home/john/immich-app/library/backups/*",
            "/home/john/immich-app/library/upload/*",
            "/home/john/immich-app/library/profile/*",
            "/home/john/immich-app/library/library/*",
        ],
        "my_secret_encryption_key"
    ));

    return;
}

if (FTPSettingsService.IsSettingsFilePresent() is false) 
{
    Console.WriteLine($"No ftp settings file present, a example one will be generated at " + FTPSettingsService.SettingFilePath);
    Console.WriteLine("");
    Console.WriteLine("Fill in the settings with your FTP credentials and then rerun the program");

    FTPSettingsService.WriteSettingsToFile(new FTPSettings(
        "ftp.example.com",
        21,
        "johnftp",
        "my_secret_ftp_password"
    ));

    return;
}

BackupSettings backupSettings = generalSettingsService.GetSettings()!;
FTPSettings FTPSettings = FTPSettingsService.GetSettings()!;


var stopwatch = Stopwatch.StartNew();


CancellationTokenSource cts = new();

WorkerManager<UploadJob> uploadManager = new(async (dispatcher) => 
{
    FTPDataStore dataStore = await FTPDataStore.GenerateNewFromSettings(FTPSettings);
    return new UploadWorker(dispatcher, dataStore);
});
uploadManager.MinWorkers = 0;
uploadManager.MaxWorkers = 10;
uploadManager.JobsPerWorker = 1;

var uploadManagerTask = await uploadManager.Run(cts.Token);



WorkerManager<ZippingJob> zippingManager = new(async (dispatcher) => 
{
    return new ZippingWorker(dispatcher, uploadManager.Dispatcher, backupSettings);
});
zippingManager.MinWorkers = 0;
zippingManager.MaxWorkers = 10;

var zippingManagerTask = await zippingManager.Run(cts.Token);



WorkerManager<FileCheckJob> fileCheckManager = new(async dispatcher => 
{
    FTPDataStore dataStore = await FTPDataStore.GenerateNewFromSettings(FTPSettings);
    return new CheckingWorker(dispatcher, zippingManager.Dispatcher, dataStore, backupSettings);
});
fileCheckManager.MinWorkers = 0;
fileCheckManager.MaxWorkers = 10;
fileCheckManager.JobsPerWorker = 1;

var fileCheckManagerTask = await fileCheckManager.Run(cts.Token);



WorkerManager<HashingJob> hashingManager = new(async (dispatcher) => 
{
    return new HashingWorker(dispatcher, fileCheckManager.Dispatcher);
});
hashingManager.MinWorkers = 0;
hashingManager.MaxWorkers = 5;

var hashingManagerTask = await hashingManager.Run(cts.Token);



PathResolver pathResolver = new();

var jobs = backupSettings.BackupLocationPatterns
    .SelectMany(pathResolver.GetFilesFromPath)
    .Select(l => new HashingJob(l));

hashingManager.Dispatcher.AddJobsToQueue(jobs);


Task[] managerTasks = [uploadManagerTask, zippingManagerTask, fileCheckManagerTask];

while (true) {
    await Task.Delay(TimeSpan.FromMilliseconds(1000));

    int totalJobsLeft = uploadManager.Dispatcher.JobsLeft() + zippingManager.Dispatcher.JobsLeft() + fileCheckManager.Dispatcher.JobsLeft() + hashingManager.Dispatcher.JobsLeft();
    
    if (totalJobsLeft == 0 && stopwatch.IsRunning) {
        stopwatch.Stop();
        Console.WriteLine($"all done, took {stopwatch.Elapsed.TotalSeconds} seconds");
    }


    Console.Clear();
    Console.WriteLine(
    $"""
    time: {stopwatch.Elapsed.TotalSeconds} seconden
    - - - - - - - - - - - - - - - - - - - - - - - - 
    hashing:        {hashingManager.GetWorkersCount(),2} workers | {hashingManager.Dispatcher.JobsLeft(),4} jobs
    checking files: {fileCheckManager.GetWorkersCount(),2} workers | {fileCheckManager.Dispatcher.JobsLeft(),4} jobs
    zipping:        {zippingManager.GetWorkersCount(),2} workers | {zippingManager.Dispatcher.JobsLeft(),4} jobs
    upload:         {uploadManager.GetWorkersCount(),2} workers | {uploadManager.Dispatcher.JobsLeft(),4} jobs
    """
    );

    var failedJobs = fileCheckManager.Dispatcher.GetFailedJobs();
    if (failedJobs.Length > 0) 
    {
        Console.WriteLine("- - - - - - - - - - - - - - - - - - - - - - - - ");
        Console.WriteLine($"Failed jobs: ");
        foreach (var job in failedJobs)
        {   
            Console.WriteLine(job.ToString());
        }
    }
}



Task.WaitAll(managerTasks);