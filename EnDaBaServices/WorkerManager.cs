using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using EnDaBaServices;

namespace EnDaBaServices;

public sealed class WorkerManager<TJob>(Func<JobDispatcher<TJob>, Task<JobWorker<TJob>>> createWorker) where TJob : JobBase
{
    public readonly JobDispatcher<TJob> Dispatcher = new();
    public int MinWorkers { get; set; } = 0;
    public int MaxWorkers { get; set;} = 5;
    public decimal JobsPerWorker { get; set; } = 10;
    public TimeSpan DetectionTimeout { get; set; } = TimeSpan.FromSeconds(1);

    private readonly Func<JobDispatcher<TJob>, Task<JobWorker<TJob>>> createWorker = createWorker;


    private readonly ConcurrentDictionary<JobWorker<TJob>, Task> staticWorkForce = [];
    private readonly ConcurrentDictionary<JobWorker<TJob>, Task> dynamicWorkForce = [];


    public async Task<Task> Run(CancellationToken cancellationToken) 
    {
        if (MinWorkers > MaxWorkers) {
            throw new InvalidOperationException($"{nameof(MinWorkers)} is bigger then {nameof(MaxWorkers)}");
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
            if (staticWorkForce.Count < MinWorkers) 
            {
                int workersToAdd = MinWorkers - staticWorkForce.Count;
                await AddNewWorkersToStaticWorkForce(workersToAdd, cancellationToken);
            }


            int maxDynamicWorkForceSize = MaxWorkers - MinWorkers;
            if (maxDynamicWorkForceSize == 0) {
                await Task.Delay(DetectionTimeout, cancellationToken);
                continue;
            }


            int jobCount = Dispatcher.JobsLeft();
            int workersNeeded = (int)((jobCount / JobsPerWorker) + 0.5m);

            if (workersNeeded <= 0 && jobCount > 0) {
                workersNeeded = 1;
            }

            workersNeeded -= MinWorkers;

            int targetWorkers = Math.Clamp(workersNeeded, 0, maxDynamicWorkForceSize);

            int currentWorkers = dynamicWorkForce.Count;
            int workersDifference = targetWorkers - currentWorkers;


            if (workersDifference > 0) 
            {
                await AddNewWorkersToDynamicWorkForce(workersDifference, cancellationToken);
            }
            else if (workersDifference < 0) 
            {
                StopWorkers(dynamicWorkForce, Math.Abs(workersDifference));
            } 

            await Task.Delay(DetectionTimeout, cancellationToken);
        } 
    }

    private async Task AddNewWorkersToDynamicWorkForce(int amount, CancellationToken cancellationToken) 
    {
        await Parallel.ForAsync(0, amount, cancellationToken, async (_,_) => 
        {
            var worker = await createWorker(Dispatcher);

            var workerTask = Task
                .Run(() => worker.StartWorking(cancellationToken))
                .ContinueWith(t => dynamicWorkForce.Remove(worker, out _), cancellationToken);

            bool status = dynamicWorkForce.TryAdd(worker, workerTask);

            if (status is false) {
                System.Console.WriteLine();
            }
        });
    }
    
    private async Task AddNewWorkersToStaticWorkForce(int amount, CancellationToken cancellationToken) 
    {
        await Parallel.ForAsync(0, amount, cancellationToken, async (_,_) => 
        {
            var worker = await createWorker(Dispatcher);

            var workerTask = Task.Factory
                .StartNew(() => worker.StartWorking(cancellationToken), TaskCreationOptions.LongRunning)
                .ContinueWith(t => staticWorkForce.Remove(worker, out _), cancellationToken);

            bool status = staticWorkForce.TryAdd(worker, workerTask);

            if (status is false) {
                System.Console.WriteLine();
            }
        });
    }
    
    private void StopWorkers(ConcurrentDictionary<JobWorker<TJob>, Task> workForce, int amount) 
    {
        var workersToRemove = workForce.Keys
            .Take(amount)
            .ToList();

        foreach (var worker in workersToRemove)
        {
            worker.StopWorking();
        }
    }
}
