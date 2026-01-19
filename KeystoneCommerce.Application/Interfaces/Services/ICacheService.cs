namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface ICacheService
    {
        T? Get<T>(string key) where T : class;
        void Remove(string key);
        void Set<T>(string key, T value, TimeSpan absoluteExpiration) where T : class;
        void Set<T>(string key, T value, TimeSpan absoluteExpiration,
            TimeSpan slidingExpiration) where T : class;
        void RemoveByPrefix(string prefix);
    }
}
