using System;

namespace EnDaBaServices;


public abstract class JobWorker<TSourceJob>
    where TSourceJob : JobBase
{
    private JobDispatcher<TSourceJob>? jobDispatcher;
    private Func<string, Task>? logErrorsTo;

    public JobWorker<TSourceJob> SetRequiredDependencies(JobDispatcher<TSourceJob> jobDispatcher, Func<string, Task> logErrorsTo) {
        this.jobDispatcher = jobDispatcher;
        this.logErrorsTo = logErrorsTo;

        return this;
    }

    public TimeSpan TimeOutWhenWaitingForJob { get; set; } = TimeSpan.FromSeconds(1);

    private bool hasBeenStopped = false;

    private void ValidateRequiredServices()
    {
        if (logErrorsTo is null) 
            throw new Exception("Cannot start worker, no error logger has been provided");

        if (jobDispatcher is null)
            throw new Exception("Cannot start worker, no job dispatcher has been provided");
    }

    public async Task StartWorking(CancellationToken cancellationToken) 
    {
        ValidateRequiredServices();

        TSourceJob? currentJob = null;
        while (cancellationToken.IsCancellationRequested is false && hasBeenStopped is false) 
        {
            try
            {
                currentJob = jobDispatcher!.GetJob();

                if (currentJob is null)
                {
                    await Task.Delay(TimeOutWhenWaitingForJob, cancellationToken);
                    continue;
                }

                await ProcessJob(currentJob, cancellationToken);
                currentJob = null;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await logErrorsTo!($"Error occurred in worker of type {typeof(TSourceJob).Name}: {ex.Message}");
                break;
            }
        }    

        if (currentJob is not null) {
            await jobDispatcher!.AddJobToQueueAsync(currentJob);
        }
    }

    public void StopWorking() {
        hasBeenStopped = true;
    }

    public abstract Task ProcessJob(TSourceJob job, CancellationToken cancellationToken);
}