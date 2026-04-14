namespace MassTransitUseCases.Contracts;

public class RefundIssued
{
    public Guid RefundId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTimeOffset IssuedAt { get; set; }
}
