namespace MassTransitUseCases.Contracts;

public class InvoiceIssued
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; set; }
}
