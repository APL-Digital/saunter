using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Producers;

[AsyncApi]
public class NotificationDigestRequester
{
    private readonly IRequestClient<NotificationDigestRequested> _requestClient;

    public NotificationDigestRequester(IRequestClient<NotificationDigestRequested> requestClient)
    {
        _requestClient = requestClient;
    }

    // Use case: document the logical reply channel and reply message while leaving the reply address unspecified.
    [Channel(CommerceChannels.NotificationDigestRequests, CommerceChannels.NotificationDigestRequestsAddress, Servers = new[] { "rabbitmq" }, Description = "Requests to prepare a digest of pending customer notifications.")]
    [SendOperation(typeof(NotificationDigestRequested), OperationId = "RequestNotificationDigest", Summary = "Request a notification digest.", Reply = CommerceChannels.NotificationDigestReplies, ReplyMessagePayloadType = typeof(NotificationDigestReady), Description = "Demonstrates the synthesized reply-channel pattern with no explicit reply address metadata.")]
    [Message(typeof(NotificationDigestRequested), Name = "NotificationDigestRequested", Title = "Notification digest requested", Summary = "Ask notification services to prepare a digest for a customer.", HeadersType = typeof(CommerceMessageHeaders), CorrelationId = "workflowCorrelation")]
    public async Task<NotificationDigestReady> Request(NotificationDigestRequested message)
    {
        var response = await _requestClient.GetResponse<NotificationDigestReady>(message);
        return response.Message;
    }
}
