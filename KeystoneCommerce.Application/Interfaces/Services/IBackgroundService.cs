using System.Linq.Expressions;

namespace KeystoneCommerce.Application.Interfaces.Services;

public interface IBackgroundService
{
    public void EnqueueJob(Expression<Action> methodCall);
    public void ScheduleJob(Expression<Action> methodCall, TimeSpan delay);
    void ScheduleJob<T>(Expression<Action<T>> methodCall, TimeSpan delay);
}