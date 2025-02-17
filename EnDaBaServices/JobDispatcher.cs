 using System;
using System.Threading.Tasks;

namespace EnDaBaServices;

public sealed class JobDispatcher<TJob>(Func<TJob, Task> onFailedJob) where TJob : JobBase
{
    public int JobAttemptLimit { get; set; } = 4;
    private readonly Func<TJob, Task> onFailedJob = onFailedJob;
    private readonly List<TJob> _jobs = [];
    private readonly Lock _jobsLock = new();

    public int JobsLeft() {
        return _jobs.Count;
    }

    public async Task AddJobToQueueAsync(TJob job) 
    {
        if (job.Attempts++ >= JobAttemptLimit) {
            await onFailedJob(job);
            return;
        }

        lock(_jobsLock)
        {
            _jobs.Add(job);
        }
    }

    public async Task AddJobsToQueue(IEnumerable<TJob> jobs) 
    {
        var group = jobs.GroupBy(j => j.Attempts++ >= JobAttemptLimit);

        TJob[] failedJobs = group.FirstOrDefault(g => g.Key == true)?.ToArray() ?? [];
        TJob[] newJobs = group.FirstOrDefault(g => g.Key == false)?.ToArray() ?? [];

        foreach (TJob job in failedJobs) {
            await onFailedJob(job);
        }

        lock(_jobsLock)
        {
            _jobs.AddRange(newJobs);
        }
    }

    public TJob? GetJob() 
    {
        lock(_jobsLock)
        {
            TJob? job = _jobs.FirstOrDefault();

            if (job is not null) {
                _jobs.Remove(job);
            }

            return job;
        }
    }
}
