using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IBannerService
    {
        Dictionary<int,string> GetBannerTypes();
    }
}
