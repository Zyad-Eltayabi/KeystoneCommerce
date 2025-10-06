using KeystoneCommerce.Application.Interfaces.Services;

namespace KeystoneCommerce.Infrastructure.Services
{
    public class ImageService : IImageService
    {
        public async Task<string> SaveImageAsync(byte[] imageData,string imageType, string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var newFileName = $"{Guid.NewGuid()}{imageType}";
            var filePath = Path.Combine(path, newFileName);
            await File.WriteAllBytesAsync(filePath, imageData);
            return newFileName;
        }

        public Task DeleteImageAsync(string imageUrl)
        {
            var fileName = Path.GetFileName(imageUrl);
            var filePath = Path.Combine("wwwroot/images", fileName);

            if (File.Exists(filePath))
                File.Delete(filePath);

            return Task.CompletedTask;
        }
    }
}

