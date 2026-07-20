#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.Consumers;
using MassTransitUseCases.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Saunter.Tests.Examples.MassTransitUseCases
{
    public class InventoryReservationConsumerTests
    {
        [Fact]
        public async Task Consume_RejectsNonPositiveQuantities()
        {
            var context = CreateContext(quantity: 0);
            var consumer = new InventoryReservationConsumer(NullLogger<InventoryReservationConsumer>.Instance);

            await consumer.Consume(context);

            var response = GetResponse<InventoryReservationRejected>(context);
            response.Reason.ShouldBe("Reservation quantity must be greater than zero.");
        }

        [Fact]
        public async Task Consume_ReservesPositiveQuantities()
        {
            var context = CreateContext(quantity: 3);
            var consumer = new InventoryReservationConsumer(NullLogger<InventoryReservationConsumer>.Instance);

            await consumer.Consume(context);

            var response = GetResponse<InventoryReserved>(context);
            response.Quantity.ShouldBe(3);
            response.ReservationId.ShouldStartWith("reservation-");
        }

        private static ConsumeContext<InventoryReservationRequested> CreateContext(int quantity)
        {
            var context = Substitute.For<ConsumeContext<InventoryReservationRequested>>();
            context.Message.Returns(new InventoryReservationRequested
            {
                OrderId = Guid.NewGuid(),
                WarehouseId = "primary",
                Sku = "sku-123",
                Quantity = quantity,
            });
            return context;
        }

        private static TResponse GetResponse<TResponse>(ConsumeContext<InventoryReservationRequested> context)
        {
            return context.ReceivedCalls()
                .SelectMany(call => call.GetArguments())
                .OfType<TResponse>()
                .Single();
        }
    }
}
