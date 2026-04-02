using System;
using Saunter.AttributeProvider;
using Saunter.AttributeProvider.Attributes;
using Saunter.Options;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider.UnitTests
{
    public class AttributeChannelBuilderTests
    {
        [Fact]
        public void Build_AddsImplicitAddressParameters()
        {
            var builder = new AttributeChannelBuilder();
            var member = typeof(ChannelFixture).GetMethod(nameof(ChannelFixture.Publish))!;
            var attribute = new ChannelAttribute("orders", "orders.{tenantId}");

            var channel = builder.Build(member, attribute, ["orderCreated"], new AsyncApiInferenceOptions());

            channel.Id.ShouldBe("orders");
            channel.MessageIds.ShouldBe(["orderCreated"]);
            channel.Parameters.Count.ShouldBe(1);
            channel.Parameters[0].Name.ShouldBe("tenantId");
        }

        [Fact]
        public void Build_ThrowsWhenDeclaredParameterIsMissingFromAddress()
        {
            var builder = new AttributeChannelBuilder();
            var member = typeof(ChannelFixture).GetMethod(nameof(ChannelFixture.PublishWithExtraParameter))!;
            var attribute = new ChannelAttribute("orders", "orders.created");

            var actual = () => builder.Build(member, attribute, [], new AsyncApiInferenceOptions());

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("ChannelFixture.PublishWithExtraParameter");
        }

        [Fact]
        public void Build_ThrowsWhenAddressContainsQueryString()
        {
            var builder = new AttributeChannelBuilder();
            var member = typeof(ChannelFixture).GetMethod(nameof(ChannelFixture.Publish))!;
            var attribute = new ChannelAttribute("orders", "orders.{tenantId}?foo=bar");

            var actual = () => builder.Build(member, attribute, [], new AsyncApiInferenceOptions());

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("ChannelFixture.Publish");
        }

        [Fact]
        public void Build_SupportsChannelParameterWithoutType()
        {
            var builder = new AttributeChannelBuilder();
            var member = typeof(ChannelFixture).GetMethod(nameof(ChannelFixture.PublishWithoutTypedParameter))!;
            var attribute = new ChannelAttribute("orders", "orders.{tenantId}");

            var channel = builder.Build(member, attribute, [], new AsyncApiInferenceOptions());

            channel.Parameters.Count.ShouldBe(1);
            channel.Parameters[0].Name.ShouldBe("tenantId");
            channel.Parameters[0].EnumValues.ShouldBeEmpty();
        }

        [Fact]
        public void Build_MapsChannelParameterDefaultAndExamples()
        {
            var builder = new AttributeChannelBuilder();
            var member = typeof(ChannelFixture).GetMethod(nameof(ChannelFixture.PublishWithParameterMetadata))!;
            var attribute = new ChannelAttribute("orders", "orders.{tenantId}");

            var channel = builder.Build(member, attribute, [], new AsyncApiInferenceOptions());

            channel.Parameters.Count.ShouldBe(1);
            channel.Parameters[0].DefaultValue.ShouldBe("tenant-default");
            channel.Parameters[0].Examples.ShouldBe(["tenant-a", "tenant-b"]);
        }

        [Fact]
        public void Build_UsesNamedChannelIdOverrideWithAddressOnlyConstructor()
        {
            var builder = new AttributeChannelBuilder();
            var member = typeof(ChannelFixture).GetMethod(nameof(ChannelFixture.Publish))!;
            var attribute = new ChannelAttribute("orders.{tenantId}") { ChannelId = "ordersByTenant" };

            var channel = builder.Build(member, attribute, ["orderCreated"], new AsyncApiInferenceOptions());

            channel.Id.ShouldBe("ordersByTenant");
            channel.Address.ShouldBe("orders.{tenantId}");
        }

        private class ChannelFixture
        {
            [ChannelParameter("tenantId", typeof(string))]
            public void Publish()
            {
            }

            [ChannelParameter("tenantId", typeof(string))]
            public void PublishWithExtraParameter()
            {
            }

            [ChannelParameter("tenantId")]
            public void PublishWithoutTypedParameter()
            {
            }

            [ChannelParameter("tenantId", typeof(string), DefaultValue = "tenant-default", Examples = new[] { "tenant-a", "tenant-b" })]
            public void PublishWithParameterMetadata()
            {
            }
        }
    }
}
