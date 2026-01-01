using Hangfire;

namespace KeystoneCommerce.Infrastructure.Services;

public class HangfireService : IBackgroundService
{
    public HangfireService()
    {
    }

    public void EnqueueJob(Expression<Action> methodCall)
    {
        BackgroundJob.Enqueue(methodCall);
    }

    public void EnqueueJob<T>(Expression<Action<T>> methodCall)
    {
        BackgroundJob.Enqueue<T>(methodCall);
    }

    public void ScheduleJob(Expression<Action> methodCall, TimeSpan delay)
    {
        BackgroundJob.Schedule(methodCall, delay);
    }

    public void ScheduleJob<T>(Expression<Action<T>> methodCall, TimeSpan delay)
    {
        BackgroundJob.Schedule<T>(methodCall, delay);
    }
}