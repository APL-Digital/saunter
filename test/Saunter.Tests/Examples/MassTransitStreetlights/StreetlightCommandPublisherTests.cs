#nullable enable
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MassTransitStreetlights.Contracts;
using MassTransitStreetlights.Producers;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Saunter.Tests.Examples.MassTransitStreetlights
{
    public class StreetlightCommandPublisherTests
    {
        [Fact]
        public async Task TurnOn_PublishesStreetlightIdInPayload()
        {
            var publishEndpoint = Substitute.For<IPublishEndpoint>();
            TurnOnOffPayload? published = null;
            publishEndpoint
                .Publish(Arg.Do<TurnOnOffPayload>(message => published = message), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            var sut = new StreetlightCommandPublisher(publishEndpoint);

            await sut.TurnOn("streetlight-1");

            published.ShouldNotBeNull();
            published.Command.ShouldBe(TurnOnOffCommand.On);
            var streetlightIdProperty = published.GetType().GetProperty("StreetlightId");
            streetlightIdProperty.ShouldNotBeNull();
            streetlightIdProperty.GetValue(published).ShouldBe("streetlight-1");
        }

        [Fact]
        public async Task Dim_PublishesStreetlightIdInPayload()
        {
            var publishEndpoint = Substitute.For<IPublishEndpoint>();
            DimLightPayload? published = null;
            publishEndpoint
                .Publish(Arg.Do<DimLightPayload>(message => published = message), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            var sut = new StreetlightCommandPublisher(publishEndpoint);

            await sut.Dim("streetlight-2", 42);

            published.ShouldNotBeNull();
            published.Percentage.ShouldBe(42);
            var streetlightIdProperty = published.GetType().GetProperty("StreetlightId");
            streetlightIdProperty.ShouldNotBeNull();
            streetlightIdProperty.GetValue(published).ShouldBe("streetlight-2");
        }
    }
}
