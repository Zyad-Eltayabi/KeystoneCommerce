using Hangfire;
using KeystoneCommerce.Application.Interfaces.Services;
using System.Linq.Expressions;

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

    public void ScheduleJob(Expression<Action> methodCall, TimeSpan delay)
    {
        BackgroundJob.Schedule(methodCall, delay);
    }

    public void ScheduleJob<T>(Expression<Action<T>> methodCall, TimeSpan delay)
    {
        BackgroundJob.Schedule<T>(methodCall, delay);
    }
}