namespace KeystoneCommerce.WebUI.ViewModels.Dashboard;

public class SystemHealthViewModel
{
    public int FailedPaymentsCount { get; set; }
    public int PendingPaymentsCount { get; set; }
    public int ActiveReservationsCount { get; set; }
    public int ExpiredReservationsCount { get; set; }
}
