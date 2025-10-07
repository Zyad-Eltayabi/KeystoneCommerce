namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IMappingService
    {
        TDestination Map<TDestination>(object source);
        TDestination Map<TSource, TDestination>(TSource source);
        void Map<TSource, TDestination>(TSource source, TDestination destination);
    }
}
