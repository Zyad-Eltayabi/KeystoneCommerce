using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeystoneCommerce.Application.DTOs
{
    public class CreateBannerDto
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Link { get; set; }
        public int Priority { get; set; }
        public int BannerType { get; set; }
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        public string ImageType { get; set; }
    }
}
