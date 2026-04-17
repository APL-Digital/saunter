using System;
using System.Linq;
using ByteBard.AsyncAPI.Bindings.AMQP;
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
                [new AsyncApiParameterDescriptor("tenantId", null, null, [], null, [])]);
            var operationDescriptor = new AsyncApiOperationDescriptor(
                AsyncApiAction.Send,
                "orders",
                null,
                "summary",
                null,
                null,
                ["orderCreated"],
                ["orders"],
                new AsyncApiOperationReplyDescriptor("orders.reply", "$message.header#/replyTo", "reply")
                {
                    MessageIds = ["orderCreated"],
                });

            mapper.RegisterMessageResolution(components, resolution);
            var channel = mapper.MapChannel(components, channelDescriptor);
            var operation = mapper.MapOperation(operationDescriptor);

            components.Messages.ShouldContainKey("orderCreated");
            components.Schemas.ShouldContainKey("orderCreatedPayload");
            channel.Messages.ShouldContainKey("orderCreated");
            channel.Parameters.ShouldContainKey("tenantId");
            operation.Messages.Single().Reference.Reference.ShouldBe("#/channels/orders/messages/orderCreated");
            operation.Reply!.Channel!.Reference.Reference.ShouldBe("#/channels/orders.reply");
            operation.Reply.Messages.Single().Reference.Reference.ShouldBe("#/channels/orders.reply/messages/orderCreated");
        }

        [Fact]
        public void MapChannel_ProjectsChannelParameterDefaultsAndExamples()
        {
            var mapper = new AsyncApiDescriptorMapper(new AsyncApiSchemaMapper());
            var components = new AsyncApiComponents();
            var channelDescriptor = new AsyncApiChannelDescriptor(
                "orders",
                "orders.{tenantId}",
                null,
                null,
                null,
                null,
                [],
                [],
                [new AsyncApiParameterDescriptor("tenantId", "desc", "path", [], "tenant-default", ["tenant-a", "tenant-b"])]);

            _ = mapper.MapChannel(components, channelDescriptor);

            components.Parameters.ShouldContainKey("tenantId");
            components.Parameters["tenantId"].Default.ShouldBe("tenant-default");
            components.Parameters["tenantId"].Examples.ShouldBe(["tenant-a", "tenant-b"]);
        }

        [Fact]
        public void MapChannel_ThrowsOnConflictingReusableParameterDefinition()
        {
            var mapper = new AsyncApiDescriptorMapper(new AsyncApiSchemaMapper());
            var components = new AsyncApiComponents
            {
                Parameters =
                {
                    ["tenantId"] = new AsyncApiParameter
                    {
                        Description = "existing-description",
                        Location = "path",
                        Default = "tenant-default",
                        Examples = ["tenant-a"],
                        Enum = ["tenant-a"],
                    }
                }
            };
            var channelDescriptor = new AsyncApiChannelDescriptor(
                "orders",
                "orders.{tenantId}",
                null,
                null,
                null,
                null,
                [],
                [],
                [new AsyncApiParameterDescriptor("tenantId", "incoming-description", "path", ["tenant-a"], "tenant-default", ["tenant-a"])]);

            var actual = () => mapper.MapChannel(components, channelDescriptor);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("Conflicting reusable parameter name 'tenantId'");
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

        [Fact]
        public void RegisterMessageResolution_InitializesSchemaCollectionWhenNull()
        {
            var mapper = new AsyncApiDescriptorMapper(new AsyncApiSchemaMapper());
            var components = new AsyncApiComponents
            {
                Schemas = null!
            };
            var resolution = new AsyncApiMessageResolutionDescriptor(
                [],
                [],
                [
                    new AsyncApiSchemaComponentDescriptor("orderCreatedPayload", new AsyncApiSchemaDescriptor { Id = "orderCreatedPayload", Type = AsyncApiSchemaValueType.Object }),
                ]);

            mapper.RegisterMessageResolution(components, resolution);

            components.Schemas.ShouldNotBeNull();
            components.Schemas.ShouldContainKey("orderCreatedPayload");
        }

        [Fact]
        public void MapAndRegister_UsesInlineBindingsWhenBindingsRefIsMissing()
        {
            var mapper = new AsyncApiDescriptorMapper(new AsyncApiSchemaMapper());
            var components = new AsyncApiComponents();
            var resolution = new AsyncApiMessageResolutionDescriptor(
                ["signupMessage"],
                [
                    new AsyncApiMessageDescriptor(
                        "signupMessage",
                        "signupMessage",
                        "Signup message",
                        null,
                        null,
                        "signupPayload",
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        [])
                    {
                        Bindings = new()
                        {
                            new AMQPMessageBinding
                            {
                                ContentEncoding = "gzip",
                                MessageType = "user.signup",
                                BindingVersion = "0.3.0",
                            }
                        }
                    }
                ],
                [
                    new AsyncApiSchemaComponentDescriptor("signupPayload", new AsyncApiSchemaDescriptor { Id = "signupPayload", Type = AsyncApiSchemaValueType.Object }),
                ]);
            var channelDescriptor = new AsyncApiChannelDescriptor(
                "signup",
                "user.signup",
                null,
                null,
                null,
                null,
                [],
                ["signupMessage"],
                [])
            {
                Bindings = new()
                {
                    new AMQPChannelBinding
                    {
                        Is = ChannelType.RoutingKey,
                        Exchange = new Exchange
                        {
                            Name = "user.events",
                            Type = ExchangeType.Topic,
                            Durable = true,
                            AutoDelete = false,
                            Vhost = "/",
                        },
                        BindingVersion = "0.3.0",
                    }
                }
            };
            var operationDescriptor = new AsyncApiOperationDescriptor(
                AsyncApiAction.Receive,
                "signup",
                null,
                null,
                null,
                null,
                ["signupMessage"],
                [],
                null)
            {
                Bindings = new()
                {
                    new AMQPOperationBinding
                    {
                        Expiration = 60000,
                        UserId = "guest",
                        Priority = 5,
                        DeliveryMode = DeliveryMode.Persistent,
                        Mandatory = true,
                        Timestamp = true,
                        Ack = true,
                        BindingVersion = "0.3.0",
                    }
                }
            };

            mapper.RegisterMessageResolution(components, resolution);
            var channel = mapper.MapChannel(components, channelDescriptor);
            var operation = mapper.MapOperation(operationDescriptor);

            components.Messages["signupMessage"].Bindings["amqp"].ShouldBeOfType<AMQPMessageBinding>();
            channel.Bindings["amqp"].ShouldBeOfType<AMQPChannelBinding>();
            operation.Bindings["amqp"].ShouldBeOfType<AMQPOperationBinding>();
        }
    }
}
