using System;

namespace ImmichEnDaBa;


public abstract class JobWorker<TSourceJob>(JobDispatcher<TSourceJob> sourceJobDispenser)
    where TSourceJob : class
{
    private readonly JobDispatcher<TSourceJob> jobDispenser = sourceJobDispenser;
    public TimeSpan TimeOutWhenWaitingForJob = TimeSpan.FromSeconds(1);

    public async Task StartWorking(CancellationToken cancellationToken) 
    {
        while (cancellationToken.IsCancellationRequested is false) 
        {
            try
            {
                TSourceJob? job = jobDispenser.GetJob();

                if (job is null)
                {
                    await Task.Delay(TimeOutWhenWaitingForJob, cancellationToken);
                    continue;
                }

                await ProcessJob(job, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }        
    }

    public abstract Task ProcessJob(TSourceJob job, CancellationToken cancellationToken);
}

