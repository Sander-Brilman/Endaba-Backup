using System;

namespace ImmichEnDaBa;

public sealed class JobDispatcher<TJob> where TJob : class
{

    private List<TJob> _jobs = [];

    private static readonly Lock _lockObject = new();

    public int JobsLeft() {
        return _jobs.Count;
    }

    public void AddJobToQueue(TJob job) 
    {
        lock(_lockObject)
        {
            _jobs.Add(job);
        }
    }

    public void AddJobsToQueue(IEnumerable<TJob> jobs) 
    {
        lock(_lockObject)
        {
            _jobs.AddRange(jobs);
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
