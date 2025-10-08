using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IImageService
    {
        Task<string> SaveImageAsync(byte[] imageData, string imageType, string path);
        Task DeleteImageAsync(string path,string imageName);
    }
}
