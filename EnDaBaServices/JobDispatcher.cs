 using System;

namespace EnDaBaServices;

public sealed class JobDispatcher<TJob> where TJob : JobBase
{
    public int JobAttemptLimit { get; set; } = 4;

    private readonly List<TJob> _failedJobs = [];

    private readonly List<TJob> _jobs = [];

    private static readonly Lock _lockObject = new();

    public TJob[] GetFailedJobs() {
        return [.. _failedJobs];
    }

    public int JobsLeft() {
        return _jobs.Count;
    }

    public void AddJobToQueue(TJob job) 
    {
        lock(_lockObject)
        {
            if (job.Attempts++ >= JobAttemptLimit) {
                _failedJobs.Add(job);
                return;
            }

            _jobs.Add(job);
        }
    }

    public void AddJobsToQueue(IEnumerable<TJob> jobs) 
    {
        lock(_lockObject)
        {
            var group = jobs.GroupBy(j => j.Attempts++ >= JobAttemptLimit);

            TJob[] newJobs = group.FirstOrDefault(g => g.Key == false)?.ToArray() ?? [];
            _jobs.AddRange(newJobs);

            TJob[] failedJobs = group.FirstOrDefault(g => g.Key == true)?.ToArray() ?? [];
            _failedJobs.AddRange(failedJobs);
        }
    }

    public TJob? GetJob() 
    {
        lock(_lockObject)
        {
            TJob? job = _jobs.FirstOrDefault();

            if (job is not null) {
                _jobs.Remove(job);
            }

            return job;
        }
    }
}
