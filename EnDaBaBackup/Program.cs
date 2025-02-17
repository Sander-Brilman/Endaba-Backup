using System.Diagnostics;
using EnDaBaServices;
using EnDaBaServices.DataStores.FTP;
using EnDaBaServices.Settings;
using EnDaBaBackup.Workers;
using EnDaBaBackup;

BackupSettings? backupSettings = SettingsChecker.GetBackupSettingsFromUser();
FTPSettings? ftpSettings = SettingsChecker.GetFTPSettingsFromUser();

if (backupSettings is null || ftpSettings is null)
{
    Console.WriteLine("re-run this program once you have edited the files");
    return;
}


LoggingService loggingService = new("endaba-error-log.txt");
CancellationTokenSource cts = new();



var uploadManager = WorkerManager<UploadJob>.CreateNew(options => {
        options.JobsPerWorker = 1;
        options.CreateWorker = async () => await UploadWorker.CreateNew(ftpSettings);
        options.LogErrorsTo = loggingService.LogError;
    });

var uploadManagerTask = await uploadManager.Run(cts.Token);


var zippingManager = WorkerManager<ZippingJob>.CreateNew(options => {
        options.CreateWorker = async () => new ZippingWorker(uploadManager.Dispatcher, backupSettings);
        options.LogErrorsTo = loggingService.LogError;
    });

var zippingManagerTask = await zippingManager.Run(cts.Token);



var fileCheckManager = WorkerManager<FileCheckJob>.CreateNew(options => {
        options.JobsPerWorker = 1;
        options.CreateWorker = async () => await CheckingWorker.CreateNew(zippingManager, ftpSettings, backupSettings);
        options.LogErrorsTo = loggingService.LogError;
    });

var fileCheckManagerTask = await fileCheckManager.Run(cts.Token);



var hashingManager = WorkerManager<HashingJob>.CreateNew(options => {
        options.CreateWorker = async () => new HashingWorker(fileCheckManager.Dispatcher);
        options.LogErrorsTo = loggingService.LogError;
    });

var hashingManagerTask = await hashingManager.Run(cts.Token);


var pathResolverTask = Task.Factory.StartNew(async () => 
{
    PathResolver pathResolver = new();

    while (cts.IsCancellationRequested is false)
    {
        var jobs = backupSettings.BackupLocationPatterns
            .SelectMany(pathResolver.GetFilesFromPath)
            .Select(l => new HashingJob(l))
            .Append(new HashingJob("home/sander/noop.txt"));

        await hashingManager.Dispatcher.AddJobsToQueue(jobs);

        await loggingService.LogInfo("backup triggered");

        await Task.Delay(TimeSpan.FromHours(backupSettings.HoursBetweenFullBackup));
    }
}, TaskCreationOptions.LongRunning);


Console.WriteLine("Would you like the live-updating screen? [y/n]");
string response = Console.ReadLine()?.ToLower() ?? "n";

if (response == "y")
{
    var stopwatch = Stopwatch.StartNew();
    while (true) {
        await Task.Delay(TimeSpan.FromMilliseconds(1000));

        int totalJobsLeft = uploadManager.Dispatcher.JobsLeft() + zippingManager.Dispatcher.JobsLeft() + fileCheckManager.Dispatcher.JobsLeft() + hashingManager.Dispatcher.JobsLeft();

        Console.Clear();
        Console.WriteLine(
        $"""
        uptime: {stopwatch.Elapsed.TotalSeconds} seconden
        - - - - - - - - - - - - - - - - - - - - - - - - 
        hashing:        {hashingManager.GetWorkersCount(),2} workers | {hashingManager.Dispatcher.JobsLeft(),4} jobs
        checking files: {fileCheckManager.GetWorkersCount(),2} workers | {fileCheckManager.Dispatcher.JobsLeft(),4} jobs
        zipping:        {zippingManager.GetWorkersCount(),2} workers | {zippingManager.Dispatcher.JobsLeft(),4} jobs
        upload:         {uploadManager.GetWorkersCount(),2} workers | {uploadManager.Dispatcher.JobsLeft(),4} jobs
        """
        );
    }
}


Task.WaitAll([uploadManagerTask, zippingManagerTask, fileCheckManagerTask, hashingManagerTask, pathResolverTask]);