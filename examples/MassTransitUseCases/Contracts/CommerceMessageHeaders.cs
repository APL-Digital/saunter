namespace MassTransitUseCases.Contracts;

public class CommerceMessageHeaders
{
    public string CorrelationId { get; set; } = string.Empty;

    public string CausationId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
}
