namespace KeystoneCommerce.Application.DTOs.Dashboard;

public class SystemHealthDto
{
    public int FailedPaymentsCount { get; set; }
    public int PendingPaymentsCount { get; set; }
    public int ActiveReservationsCount { get; set; }
    public int ExpiredReservationsCount { get; set; }
}
