using AutoMapper;
using KeystoneCommerce.Infrastructure.Profiles;
using Microsoft.Extensions.Logging.Abstractions;

namespace KeystoneCommerce.Tests.Helpers;

public class MapperHelper
{
    public static IMapper CreateMapper()
    {
        var configurationExpression = new MapperConfigurationExpression();
        configurationExpression.AddMaps(typeof(InfrastructureMappings).Assembly);
        var config = new MapperConfiguration(configurationExpression, NullLoggerFactory.Instance);
        //config.AssertConfigurationIsValid();
        return config.CreateMapper();
    }

}