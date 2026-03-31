using System;
using System.Linq;
using ByteBard.AsyncAPI.Models;
using Saunter.AttributeProvider;
using Saunter.AttributeProvider.Descriptors;
using Saunter.SharedKernel;
using Saunter.SharedKernel.Descriptors;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider.UnitTests
{
    public class AsyncApiDescriptorMapperTests
    {
        [Fact]
        public void MapAndRegister_ProjectsDescriptorsToByteBardModels()
        {
            var mapper = new AsyncApiDescriptorMapper(new AsyncApiSchemaMapper());
            var components = new AsyncApiComponents();
            var resolution = new AsyncApiMessageResolutionDescriptor(
                ["orderCreated"],
                [
                    new AsyncApiMessageDescriptor(
                        "orderCreated",
                        "orderCreated",
                        "orderCreated",
                        "summary",
                        null,
                        "orderCreatedPayload",
                        "orderCreatedHeaders",
                        "orderCorrelation",
                        "application/json",
                        "https://example.com/message",
                        "docs",
                        "messageBinding",
                        ["orders"])
                ],
                [
                    new AsyncApiSchemaComponentDescriptor("orderCreatedPayload", new AsyncApiSchemaDescriptor { Id = "orderCreatedPayload", Type = AsyncApiSchemaValueType.Object }),
                    new AsyncApiSchemaComponentDescriptor("orderCreatedHeaders", new AsyncApiSchemaDescriptor { Id = "orderCreatedHeaders", Type = AsyncApiSchemaValueType.Object }),
                ]);
            var channelDescriptor = new AsyncApiChannelDescriptor(
                "orders",
                "orders.{tenantId}",
                null,
                null,
                null,
                null,
                ["primary"],
                ["orderCreated"],
                [new AsyncApiParameterDescriptor("tenantId", null, null, [])]);
            var operationDescriptor = new AsyncApiOperationDescriptor(
                AsyncApiAction.Send,
                "orders",
                null,
                "summary",
                null,
                null,
                ["orderCreated"],
                ["orders"],
                new AsyncApiOperationReplyDescriptor("orders.reply", "$message.header#/replyTo", "reply"));

            mapper.RegisterMessageResolution(components, resolution);
            var channel = mapper.MapChannel(components, channelDescriptor);
            var operation = mapper.MapOperation(operationDescriptor);

            components.Messages.ShouldContainKey("orderCreated");
            components.Schemas.ShouldContainKey("orderCreatedPayload");
            channel.Messages.ShouldContainKey("orderCreated");
            channel.Parameters.ShouldContainKey("tenantId");
            operation.Messages.Single().Reference.Reference.ShouldBe("#/channels/orders/messages/orderCreated");
            operation.Reply!.Channel!.Reference.Reference.ShouldBe("#/channels/orders.reply");
        }

        [Fact]
        public void RegisterMessageResolution_ThrowsClearErrorForInvalidExternalDocsUrl()
        {
            var mapper = new AsyncApiDescriptorMapper(new AsyncApiSchemaMapper());
            var components = new AsyncApiComponents();
            var resolution = new AsyncApiMessageResolutionDescriptor(
                ["orderCreated"],
                [
                    new AsyncApiMessageDescriptor(
                        "orderCreated",
                        "orderCreated",
                        "orderCreated",
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        "not-a-valid-url",
                        null,
                        null,
                        [])
                ],
                []);

            var actual = () => mapper.RegisterMessageResolution(components, resolution);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("ExternalDocs");
        }
    }
}
