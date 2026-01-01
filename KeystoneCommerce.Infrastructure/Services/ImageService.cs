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

        public Task DeleteImageAsync(string path,string imageName)
        {
            var filePath = Path.Combine(path, imageName);
            if (File.Exists(filePath))
                File.Delete(filePath);
            return Task.CompletedTask;
        }
    }
}

