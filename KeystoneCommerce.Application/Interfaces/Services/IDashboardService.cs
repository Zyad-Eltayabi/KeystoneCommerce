using KeystoneCommerce.Application.DTOs.Dashboard;

namespace KeystoneCommerce.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
}
