namespace MassTransitUseCases.Contracts;

public class PaymentCaptured
{
    public Guid PaymentId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTimeOffset CapturedAt { get; set; }
}
