using System.Diagnostics;
using EnDaBaServices.DataStores.FTP;
using ImmichEnDaBa;
using ImmichEnDaBa.DataStores.FTP;
using ImmichEnDaBa.Settings;
using ImmichEnDaBa.Workers;

SettingsService<AppSettings> generalSettingsService = new("general-settings.json");
SettingsService<FTPSettings> FTPSettingsService = new("ftp-settings.json");

if (generalSettingsService.IsSettingsFilePresent() is false) 
{
    Console.WriteLine($"No general settings file present, a example one will be generated at " + generalSettingsService.SettingFilePath);
    Console.WriteLine("");
    Console.WriteLine("Fill in the settings and then rerun the program");

    generalSettingsService.WriteSettingsToFile(new AppSettings(
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

AppSettings appSettings = generalSettingsService.GetSettings()!;
FTPSettings FTPSettings = FTPSettingsService.GetSettings()!;



// var stopwatch = Stopwatch.StartNew();

// var store = await FTPDataStore.GenerateNewFromSettings(FTPSettings);

// AllInOneBackupService backupService = new(appSettings, store);
// await backupService.TriggerBackup();

// stopwatch.Stop();

// Console.WriteLine($"all done, took {stopwatch.Elapsed.TotalSeconds} seconds");




var stopwatch = Stopwatch.StartNew();

JobDispatcher<FileCheckJob> checkJobDispenser = new();
JobDispatcher<ZippingJob> zipJobDispenser = new();
JobDispatcher<UploadJob> uploadJobDispenser = new();


PathResolver pathResolver = new();

var jobs = appSettings.BackupLocationPatterns
    .SelectMany(pathResolver.GetFilesFromPath)
    .Select(l => new FileCheckJob(l));

checkJobDispenser.AddJobsToQueue(jobs);

CancellationTokenSource cts = new();


Task checkingWorkerTask = Parallel.ForAsync(0, 10, async (i, _) => 
{
    FTPDataStore dataStore = await FTPDataStore.GenerateNewFromSettings(FTPSettings);
    CheckingWorker checkingWorker = new(checkJobDispenser, zipJobDispenser, dataStore, appSettings);
    await checkingWorker.StartWorking(cts.Token);
});

Task zippingWorkerTask = Parallel.ForAsync(0, 10, async (i, _) => 
{
    ZippingWorker zippingWorker = new(zipJobDispenser, uploadJobDispenser, appSettings);
    await zippingWorker.StartWorking(cts.Token);
});


Task uploadWorkerTask = Parallel.ForAsync(0, 10, async (i, _) => 
{
    FTPDataStore dataStore = await FTPDataStore.GenerateNewFromSettings(FTPSettings);
    UploadWorker uploadWorker = new(uploadJobDispenser, dataStore);
    await uploadWorker.StartWorking(cts.Token);
});


while (true) {
    await Task.Delay(TimeSpan.FromSeconds(1));

    int totalJobsLeft = checkJobDispenser.JobsLeft() + zipJobDispenser.JobsLeft() + uploadJobDispenser.JobsLeft();
    
    if (totalJobsLeft == 0) {
        break;
    }

    // Console.Clear();
    Console.WriteLine("");
    Console.WriteLine($"checking jobs left {checkJobDispenser.JobsLeft()}");
    Console.WriteLine($"zipping jobs left {zipJobDispenser.JobsLeft()}");
    Console.WriteLine($"upload jobs left {uploadJobDispenser.JobsLeft()}");

}

stopwatch.Stop();
Console.WriteLine($"all done, took {stopwatch.Elapsed.TotalSeconds} seconds");


Task.WaitAll(checkingWorkerTask, zippingWorkerTask, uploadWorkerTask);