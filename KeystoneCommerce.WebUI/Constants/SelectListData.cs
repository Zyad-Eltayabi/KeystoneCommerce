using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeystoneCommerce.WebUI.Constants;

public static class SelectListData
{
    public static readonly List<SelectListItem> PageSizes = new()
    {
        new SelectListItem { Text = "10", Value = "10" },
        new SelectListItem { Text = "20", Value = "20" },
        new SelectListItem { Text = "30", Value = "30" },
    };
}