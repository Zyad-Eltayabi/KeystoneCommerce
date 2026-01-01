namespace KeystoneCommerce.Application.Notifications.Contracts;

public class EmailMessage
{
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public NotificationType NotificationType { get; set; }
}
