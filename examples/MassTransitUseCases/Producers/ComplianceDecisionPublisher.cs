using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Producers;

[AsyncApi]
public class ComplianceDecisionPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ComplianceDecisionPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    // Use case: reuse one CLR payload shape for several semantic AsyncAPI messages by assigning distinct message keys.
    [Channel(CommerceChannels.ComplianceDecisions, CommerceChannels.ComplianceDecisionsAddress, Servers = new[] { "rabbitmq", "inmemory" }, Description = "Compliance decisions emitted after case review.")]
    [SendOperation(OperationId = "PublishComplianceDecision", Summary = "Publish compliance decision messages.", Description = "Demonstrates distinct message identities backed by the same CLR payload type.")]
    [Message(typeof(ComplianceDecisionEnvelope), MessageId = "complianceApproved", Name = "ComplianceApproved", Title = "Compliance approved", Summary = "A compliance case was approved.")]
    [Message(typeof(ComplianceDecisionEnvelope), MessageId = "complianceRejected", Name = "ComplianceRejected", Title = "Compliance rejected", Summary = "A compliance case was rejected.")]
    public Task Publish(ComplianceDecisionEnvelope message)
    {
        return _publishEndpoint.Publish(message);
    }
}
