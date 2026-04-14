using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Consumers;

[AsyncApi]
public interface ICustomerPreferenceChangedConsumer
{
    // Use case: define the receive-side messaging contract on an interface when multiple implementations could exist.
    [Channel(CommerceChannels.CustomerPreferencesAddress, ChannelId = CommerceChannels.CustomerPreferences, Servers = new[] { "rabbitmq" }, Description = "Customer preference changes emitted by profile management.")]
    [ReceiveOperation(typeof(CustomerPreferenceChanged), OperationId = "HandleCustomerPreferenceChanged", Summary = "Handle a customer preference change.")]
    [Message(typeof(CustomerPreferenceChanged), Name = "CustomerPreferenceChanged", Title = "Customer preference changed", Summary = "A stored customer preference changed.")]
    Task Consume(ConsumeContext<CustomerPreferenceChanged> context);
}
