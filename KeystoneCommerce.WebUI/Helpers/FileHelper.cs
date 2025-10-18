namespace KeystoneCommerce.WebUI.Helpers
{
    public static class FileHelper
    {
        public static byte[] ConvertIFormFileToByteArray(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Array.Empty<byte>();

            using var memoryStream = new MemoryStream();
            file.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        public static string GetImageFileExtension(IFormFile file)
        {
            return Path.GetExtension(file.FileName);
        }
    }
}
