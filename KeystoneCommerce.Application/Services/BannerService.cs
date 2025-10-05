using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Domain.Enums;

namespace KeystoneCommerce.Application.Services
{
    public class BannerService : IBannerService
    {
        public Dictionary<int, string> GetBannerTypes()
        {
            Dictionary<int, string> result = new();
            var bannerEnumValues = Enum.GetValues(typeof(BannerType));
            foreach (BannerType bannerEnumValue in bannerEnumValues)
                result.Add((int)bannerEnumValue, bannerEnumValue.ToString());
            return result;
        }
    }
}
