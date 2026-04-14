namespace MassTransitUseCases.Contracts;

public class InvoicePaid
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTimeOffset PaidAt { get; set; }
}
