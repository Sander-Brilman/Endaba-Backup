using System;

namespace EnDaBaServices;


public abstract class JobWorker<TSourceJob>(JobDispatcher<TSourceJob> sourceJobDispatcher)
    where TSourceJob : JobBase
{
    private readonly JobDispatcher<TSourceJob> jobDispatcher = sourceJobDispatcher;

    public TimeSpan TimeOutWhenWaitingForJob { get; set; } = TimeSpan.FromSeconds(1);

    private bool hasBeenStopped = false;

    private TSourceJob? currentJob = null;

    public async Task StartWorking(CancellationToken cancellationToken) 
    {
        while (cancellationToken.IsCancellationRequested is false && hasBeenStopped is false) 
        {
            try
            {
                currentJob = jobDispatcher.GetJob();

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
                Console.WriteLine($"An error occurred: {ex.Message}, stopping worker");
                break;
            }
        }    

        if (currentJob is not null) {
            jobDispatcher.AddJobToQueue(currentJob);
        }
    }

    public void StopWorking() {
        hasBeenStopped = true;
    }

    public abstract Task ProcessJob(TSourceJob job, CancellationToken cancellationToken);
}

