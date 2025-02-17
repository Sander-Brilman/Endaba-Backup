using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using EnDaBaServices;

namespace EnDaBaServices;

public sealed class WorkerManager<TJob> 
    where TJob : JobBase
{
    public JobDispatcher<TJob> Dispatcher;
    private WorkerManagerSettings<TJob> settings;


    private WorkerManager(WorkerManagerSettings<TJob> settings)
    {
        Dispatcher = new(LogFailedJob);
        this.settings = settings;
    }


    private readonly ConcurrentDictionary<JobWorker<TJob>, Task> staticWorkForce = [];
    private readonly ConcurrentDictionary<JobWorker<TJob>, Task> dynamicWorkForce = [];


    private async Task LogFailedJob(TJob job)
    {
        await settings.LogErrorsTo($"Job failed: {job}");
    }


    public async Task<Task> Run(CancellationToken cancellationToken) 
    {
        if (settings.MinWorkers > settings.MaxWorkers) {
            throw new InvalidOperationException($"{nameof(settings.MinWorkers)} is bigger then {nameof(settings.MaxWorkers)}");
        }

        if (settings.CreateWorker is null) {
            throw new InvalidOperationException($"{nameof(settings.CreateWorker)} Cannot be null, please provide a async function that returns a new worker");
        }

        Task task = await Task.Factory.StartNew(async () => await DynamicallyScaleWorkForce(cancellationToken), TaskCreationOptions.LongRunning);
        return task;
    }

    public int GetWorkersCount() {
        return dynamicWorkForce.Count + staticWorkForce.Count;
    }

    private async Task DynamicallyScaleWorkForce(CancellationToken cancellationToken) 
    {
        while (cancellationToken.IsCancellationRequested is false) 
        {
            if (staticWorkForce.Count < settings.MinWorkers) 
            {
                int workersToAdd = settings.MinWorkers - staticWorkForce.Count;
                await AddNewWorkersToStaticWorkForce(workersToAdd, cancellationToken);
            }


            int jobCount = Dispatcher.JobsLeft();
            int currentDynamicWorkers = dynamicWorkForce.Count;
            if (jobCount == 0 && currentDynamicWorkers == 0) {
                await Task.Delay(settings.DetectionTimeout, cancellationToken);
                continue;
            }


            int maxDynamicWorkForceSize = settings.MaxWorkers - settings.MinWorkers;
            if (maxDynamicWorkForceSize == 0) {
                await Task.Delay(settings.DetectionTimeout, cancellationToken);
                continue;
            }


            int workersNeeded = (int)Math.Ceiling(jobCount / settings.JobsPerWorker);
            if (workersNeeded <= 0 && jobCount > 0) {
                workersNeeded = 1;
            }

            workersNeeded -= settings.MinWorkers;

            int targetWorkers = Math.Clamp(workersNeeded, 0, maxDynamicWorkForceSize);
            int workersDifference = targetWorkers - currentDynamicWorkers;


            if (workersDifference > 0) 
            {
                await AddNewWorkersToDynamicWorkForce(workersDifference, cancellationToken);
            }
            else if (workersDifference < 0) 
            {
                StopWorkers(dynamicWorkForce, Math.Abs(workersDifference));
            } 

            await Task.Delay(settings.DetectionTimeout, cancellationToken);
        } 
    }

    private async Task AddNewWorkersToDynamicWorkForce(int amount, CancellationToken cancellationToken) 
    {
        await Parallel.ForAsync(0, amount, cancellationToken, async (_,_) => 
        {
            var worker = (await settings.CreateWorker!.Invoke())
                .SetRequiredDependencies(Dispatcher, settings.LogErrorsTo);

            var workerTask = Task
                .Run(() => worker.StartWorking(cancellationToken), cancellationToken)
                .ContinueWith(t => dynamicWorkForce.Remove(worker, out _), cancellationToken);

            dynamicWorkForce.TryAdd(worker, workerTask);
        });
    }
    
    private async Task AddNewWorkersToStaticWorkForce(int amount, CancellationToken cancellationToken) 
    {
        await Parallel.ForAsync(0, amount, cancellationToken, async (_,_) => 
        {
            var worker = (await settings.CreateWorker!.Invoke())
                .SetRequiredDependencies(Dispatcher, settings.LogErrorsTo);

            var workerTask = Task.Factory
                .StartNew(() => worker.StartWorking(cancellationToken), TaskCreationOptions.LongRunning)
                .ContinueWith(t => staticWorkForce.Remove(worker, out _), cancellationToken);

            staticWorkForce.TryAdd(worker, workerTask);
        });
    }
    
    private static void StopWorkers(ConcurrentDictionary<JobWorker<TJob>, Task> workForce, int amount) 
    {
        var workersToRemove = workForce.Keys
            .Take(amount)
            .ToList();

        foreach (var worker in workersToRemove)
        {
            worker.StopWorking();
        }
    }

    public static WorkerManager<TJob> CreateNew(Action<WorkerManagerSettings<TJob>> configureSettingCallback) 
    {
        WorkerManagerSettings<TJob> settings = new();
        configureSettingCallback(settings);
        return new(settings);
    }
}

public sealed class WorkerManagerSettings<TJob>()
    where TJob : JobBase
{
    public decimal JobsPerWorker { get; set; } = 10;
    public int MinWorkers { get; set; } = 0;
    public int MaxWorkers { get; set;} = 5;
    public TimeSpan DetectionTimeout { get; set; } = TimeSpan.FromSeconds(15);
    public Func<Task<JobWorker<TJob>>>? CreateWorker { get; set; }
    public Func<string, Task> LogErrorsTo { get; set; } = (error) => 
    { 
        Console.WriteLine(error); 
        return Task.CompletedTask; 
    };
}