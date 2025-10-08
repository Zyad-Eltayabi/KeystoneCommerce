using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeystoneCommerce.Application.DTOs.Banner;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IBannerService
    {
        Dictionary<int,string> GetBannerTypes();
       Task<Result<bool>> Create(CreateBannerDto createBannerDto);
       Task<List<BannerDto>> GetBanners();
    }
}
