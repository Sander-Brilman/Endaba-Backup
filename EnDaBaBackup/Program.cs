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
    Console.WriteLine("");
    Console.WriteLine($"time: {stopwatch.Elapsed.TotalSeconds} seconden");
    Console.WriteLine($"hashing: {hashingManager.GetWorkersCount()} workers and {hashingManager.Dispatcher.JobsLeft()} jobs");
    Console.WriteLine($"checking files: {fileCheckManager.GetWorkersCount()} workers and {fileCheckManager.Dispatcher.JobsLeft()} jobs");
    Console.WriteLine($"zipping: {zippingManager.GetWorkersCount()} workers and {zippingManager.Dispatcher.JobsLeft()} jobs");
    Console.WriteLine($"upload: {uploadManager.GetWorkersCount()} workers and {uploadManager.Dispatcher.JobsLeft()} jobs");
    Console.WriteLine($"Failed jobs: ");

    var failedJobs = fileCheckManager.Dispatcher.GetFailedJobs();

    foreach (var job in failedJobs)
    {   
        Console.WriteLine(job.ToString());
    }
}



Task.WaitAll(managerTasks);