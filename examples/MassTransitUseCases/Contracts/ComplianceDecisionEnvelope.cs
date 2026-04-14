namespace MassTransitUseCases.Contracts;

public class ComplianceDecisionEnvelope
{
    public string CaseId { get; set; } = string.Empty;

    public string Decision { get; set; } = string.Empty;

    public DateTimeOffset DecidedAt { get; set; }
}
