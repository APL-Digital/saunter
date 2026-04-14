using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.Contracts;
using Microsoft.Extensions.Logging;

namespace MassTransitUseCases.Consumers;

public class CustomerPreferenceChangedConsumer :
    ICustomerPreferenceChangedConsumer,
    IConsumer<CustomerPreferenceChanged>
{
    private readonly ILogger<CustomerPreferenceChangedConsumer> _logger;

    public CustomerPreferenceChangedConsumer(ILogger<CustomerPreferenceChangedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<CustomerPreferenceChanged> context)
    {
        _logger.LogInformation("Preference {PreferenceName} changed for customer {CustomerId}", context.Message.PreferenceName, context.Message.CustomerId);
        return Task.CompletedTask;
    }
}
