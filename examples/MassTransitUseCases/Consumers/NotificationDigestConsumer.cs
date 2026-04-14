using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Consumers;

[AsyncApi]
public class NotificationDigestConsumer : IConsumer<NotificationDigestRequested>
{
    // Use case: the receive side of request/reply where only the logical reply message is documented.
    [Channel(CommerceChannels.NotificationDigestRequests, CommerceChannels.NotificationDigestRequestsAddress, Servers = new[] { "rabbitmq" }, Description = "Requests to prepare a digest of pending customer notifications.")]
    [ReceiveOperation(typeof(NotificationDigestRequested), OperationId = "HandleNotificationDigestRequest", Summary = "Handle a notification digest request.", Reply = CommerceChannels.NotificationDigestReplies, ReplyMessagePayloadType = typeof(NotificationDigestReady), Description = "Demonstrates the synthesized reply-channel pattern with no explicit reply address metadata.")]
    [Message(typeof(NotificationDigestRequested), Name = "NotificationDigestRequested", Title = "Notification digest requested", Summary = "Ask notification services to prepare a digest for a customer.", HeadersType = typeof(CommerceMessageHeaders), CorrelationId = "workflowCorrelation")]
    public async Task Consume(ConsumeContext<NotificationDigestRequested> context)
    {
        await context.RespondAsync(new NotificationDigestReady
        {
            CustomerId = context.Message.CustomerId,
            NotificationCount = 4,
            GeneratedAt = DateTimeOffset.UtcNow,
        });
    }
}
