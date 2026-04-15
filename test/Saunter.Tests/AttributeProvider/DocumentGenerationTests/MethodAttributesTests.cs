using System;
using System.Linq;
using ByteBard.AsyncAPI.Bindings.Kafka;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Saunter.AttributeProvider.Attributes;
using Saunter.AttributeProvider.Descriptors;
using Saunter.Options.Filters;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider.DocumentGenerationTests
{
    public class MethodAttributesTests
    {
        [Fact]
        public void GenerateDocument_GeneratesDocumentWithMultipleMessagesPerChannel()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(TenantMessagePublisher));

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var channel = document.AssertAndGetChannel("asw.tenant_service.tenants_history", "asw.tenant_service.tenants_history");
            document.AssertChannelMessages(channel, "anyTenantCreated", "anyTenantUpdated", "anyTenantRemoved");

            var send = document.AssertAndGetOperation("TenantMessagePublisher", AsyncApiAction.Send);
            document.AssertByMessage(send, "anyTenantCreated", "anyTenantUpdated", "anyTenantRemoved");
        }

        [Fact]
        public void GenerateDocument_GeneratesDocumentWithKafkaOperationBinding()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(TenantMessagePublisherWithBind));

            options.AsyncApi.Components = new()
            {
                OperationBindings =
                {
                    ["sample_kafka"] = new()
                    {
                        new KafkaOperationBinding
                        {
                            ClientId = new()
                            {
                                Type = SchemaType.Integer,
                            }
                        }
                    }
                }
            };

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var channel = document.AssertAndGetChannel("asw.tenant_service.tenants_history.with_bind", "asw.tenant_service.tenants_history.with_bind");
            var send = document.AssertAndGetOperation("TenantMessagePublisher", AsyncApiAction.Send);

            var bindingsReference = send.Bindings.ShouldBeOfType<AsyncApiBindingsReference<IOperationBinding>>();
            bindingsReference.Reference.Reference.ShouldBe("#/components/operationBindings/sample_kafka");
            document.AssertByMessage(send, "anyTenantCreated");
            document.Components.OperationBindings.ShouldContainKey("sample_kafka");
            document.Components.OperationBindings["sample_kafka"]["kafka"].ShouldBeOfType<KafkaOperationBinding>();
        }

        [Fact]
        public void GenerateDocument_ResolvesMessagesPerOperationOnMethod()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(MixedOperationMethodPublisher));

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var channel = document.AssertAndGetChannel("mixed.operation.method", "mixed.operation.method");
            document.AssertChannelMessages(channel, "anyTenantCreated", "anyTenantUpdated");

            var send = document.AssertAndGetOperation("MethodSend", AsyncApiAction.Send);
            document.AssertByMessage(send, "anyTenantCreated");

            var receive = document.AssertAndGetOperation("MethodReceive", AsyncApiAction.Receive);
            document.AssertByMessage(receive, "anyTenantUpdated");
        }

        [Fact]
        public void GenerateDocument_AssertByMessage_DoesNotAssumeSchemaKeyMatchesMessageId()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(CustomMessageIdPublisher));

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var send = document.AssertAndGetOperation("CustomMessageIdPublisher", AsyncApiAction.Send);

            Should.NotThrow(() => document.AssertByMessage(send, "tenant-created-event"));
        }

        [Fact]
        public void GenerateDocument_InfersOperationIdFromMemberName()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(OperationIdInferencePublisher));

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            document.AssertAndGetOperation("PublishTenantCreated", AsyncApiAction.Send);
        }

        [Fact]
        public void GenerateDocument_InfersChannelIdFromAddress()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(ChannelIdInferencePublisher));

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var channel = document.AssertAndGetChannel("tenantCreated", "tenant.created");
            document.AssertAndGetOperation("Publish", AsyncApiAction.Send).ChannelId.ShouldBe(channel.Id);
        }

        [Fact]
        public void GenerateDocument_InfersPayloadFromMethodSignature()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(SignatureInferencePublisher));

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var send = document.AssertAndGetOperation("Publish", AsyncApiAction.Send);
            document.AssertByMessage(send, "signatureInferredPayload");

            document.Components.Messages["signatureInferredPayload"].Title.ShouldBe("Signature Inferred Payload");
        }

        [Fact]
        public void GenerateDocument_InfersPayloadFromConsumeContext()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(ConsumeContextInferenceConsumer));

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var receive = document.AssertAndGetOperation("Consume", AsyncApiAction.Receive);
            document.AssertByMessage(receive, "consumeContextPayload");
        }

        [Fact]
        public void GenerateDocument_InfersAddressFromAspNetRouteWhenMissing()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(RouteInferredPublisher));

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            document.AssertAndGetChannel("tenantsCreated", "api/tenants/{tenantId}/created");
        }

        [Fact]
        public void GenerateDocument_ExplicitValuesWinOverInference()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(ExplicitOverridesInferencePublisher));

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var channel = document.AssertAndGetChannel("customChannel", "tenant.created");
            var send = document.AssertAndGetOperation("customOperation", AsyncApiAction.Send);
            document.AssertByMessage(send, "customMessage");

            send.ChannelId.ShouldBe(channel.Id);
            document.Components.Messages["customMessage"].Title.ShouldBe("Custom title");
        }

        [Fact]
        public void GenerateDocument_MapsChannelTagsFromAttribute()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(ChannelTagsPublisher));

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var channel = document.AssertAndGetChannel("tenant.tags", "tenant.tags");
            channel.Tags.Select(tag => tag.Name).ShouldBe(new[] { "billing", "events" }, ignoreOrder: true);
        }

        [Fact]
        public void GenerateDocument_GeneratesDistinctReplyChannelAndMessage()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(RequestReplyPublisher));

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();

            var requestChannel = document.AssertAndGetChannel("orders.create", "orders.create");
            document.AssertChannelMessages(requestChannel, "createOrderRequest");

            var replyChannel = document.AssertAndGetChannel("orders.create.reply", null);
            document.AssertChannelMessages(replyChannel, "createOrderAccepted");

            var receive = document.AssertAndGetOperation("CreateOrder", AsyncApiAction.Receive);
            document.AssertByMessage(receive, "createOrderRequest");
            receive.Reply.ShouldNotBeNull();
            receive.Reply.ChannelId.ShouldBe("orders.create.reply");
            receive.Reply.MessageIds.Single().ShouldBe("createOrderAccepted");
            receive.Reply.AddressLocation.ShouldBe("$message.header#/replyTo");
        }

        [Fact]
        public void GenerateDocument_AllowsReplyMessageMetadataOverrides()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(ReplyMessageOverridePublisher));

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();

            var replyChannel = document.AssertAndGetChannel("orders.override.reply", null);
            document.AssertChannelMessages(replyChannel, "customReplyMessage");

            var receive = document.AssertAndGetOperation("ReplyMessageOverrides", AsyncApiAction.Receive);
            receive.Reply.ShouldNotBeNull();
            receive.Reply.ChannelId.ShouldBe("orders.override.reply");
            receive.Reply.MessageIds.ShouldBe(["customReplyMessage"]);

            var replyMessage = document.Components.Messages["customReplyMessage"];
            replyMessage.Name.ShouldBe("Contracts:CreateOrderAccepted");
            replyMessage.Title.ShouldBe("Create Order Accepted");
        }

        [Fact]
        public void GenerateDocument_GeneratesPlaceholderReplyChannelWhenReplyHasMessagesButNoAddressMetadata()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(ReplyWithoutAddressPublisher));

            var document = documentProvider.GetDocument("reply-without-address", options);

            document.ShouldNotBeNull();

            var replyChannel = document.AssertAndGetChannel("orders.reply-without-address.reply", null);
            document.AssertChannelMessages(replyChannel, "createOrderAccepted");

            var receive = document.AssertAndGetOperation("ReplyWithoutAddress", AsyncApiAction.Receive);
            receive.Reply.ShouldNotBeNull();
            receive.Reply.ChannelId.ShouldBe("orders.reply-without-address.reply");
            receive.Reply.MessageIds.ShouldBe(["createOrderAccepted"]);
            receive.Reply.AddressLocation.ShouldBeNull();
        }

        [Fact]
        public void GenerateDocument_UsesFilteredReplyMessageIdsWhenSynthesizingReplyChannel()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(RequestReplyPublisher));
            options.AddOperationFilter<ReplaceReplyMessageIdsOperationFilter>();

            var document = documentProvider.GetDocument(null, options);

            var replyChannel = document.AssertAndGetChannel("orders.create.reply", null);
            document.AssertChannelMessages(replyChannel, "createOrderRequest");

            var receive = document.AssertAndGetOperation("CreateOrder", AsyncApiAction.Receive);
            receive.Reply.ShouldNotBeNull();
            receive.Reply.MessageIds.ShouldBe(["createOrderRequest"]);
        }

        [Fact]
        public void GenerateDocument_AppliesChannelFiltersToSynthesizedReplyChannels()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(RequestReplyPublisher));
            options.AddAsyncApiChannelFilter<ReplyChannelContextTaggingFilter>();

            var document = documentProvider.GetDocument(null, options);

            var replyChannel = document.AssertAndGetChannel("orders.create.reply", null);
            replyChannel.Tags.Select(tag => tag.Name).ShouldContain("filtered-reply-channel");
        }

        [Fact]
        public void GenerateDocument_UsesConfiguredReplyChannelAddressWhenSet()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(ReplyChannelAddressPublisher));

            var document = documentProvider.GetDocument("static-reply-address", options);

            document.ShouldNotBeNull();

            var replyChannel = document.AssertAndGetChannel("orders.static-reply", "orders.static.reply.address");
            document.AssertChannelMessages(replyChannel, "createOrderAccepted");

            var receive = document.AssertAndGetOperation("StaticReplyAddress", AsyncApiAction.Receive);
            receive.Reply.ShouldNotBeNull();
            receive.Reply.ChannelId.ShouldBe("orders.static-reply");
            receive.Reply.MessageIds.ShouldBe(["createOrderAccepted"]);
            receive.Reply.AddressLocation.ShouldBeNull();
        }

        [Fact]
        public void GenerateDocument_ThrowsWhenReplyAddressIsConfiguredWithoutReplyChannel()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(InvalidReplyAddressPublisher));

            var actual = () => documentProvider.GetDocument("negative-reply-metadata", options);

            var exception = Should.Throw<InvalidOperationException>(actual);
            exception.Message.ShouldContain("InvalidReplyAddressPublisher");
            exception.Message.ShouldContain("Consume");
        }

        [Fact]
        public void GenerateDocument_ThrowsWhenReplyAddressAndReplyChannelAddressAreBothConfigured()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(ConflictingReplyMetadataPublisher));

            var actual = () => documentProvider.GetDocument("negative-reply-metadata", options);

            var exception = Should.Throw<InvalidOperationException>(actual);
            exception.Message.ShouldContain("ConflictingReplyMetadataPublisher");
            exception.Message.ShouldContain("Consume");
            exception.Message.ShouldContain("ReplyChannelAddress");
            exception.Message.ShouldContain("ReplyAddressLocation");
        }

        [AsyncApi]
        [Channel("asw.tenant_service.tenants_history", "asw.tenant_service.tenants_history", Description = "Tenant events.")]
        [SendOperation(OperationId = "TenantMessagePublisher", Summary = "Send domains events about tenants.")]
        public class TenantMessagePublisher
        {
            [Message(typeof(AnyTenantCreated))]
            [Message(typeof(AnyTenantUpdated))]
            [Message(typeof(AnyTenantRemoved))]
            public void PublishTenantEvent<TEvent>(Guid tenantId, TEvent @event)
                where TEvent : IEvent
            {
            }
        }

        [AsyncApi]
        [Channel("asw.tenant_service.tenants_history.with_bind", "asw.tenant_service.tenants_history.with_bind", Description = "Tenant events.")]
        [SendOperation(OperationId = "TenantMessagePublisher", Summary = "Send domains events about tenants.", BindingsRef = "sample_kafka")]
        public class TenantMessagePublisherWithBind : ITenantMessagePublisher
        {
            [Message(typeof(AnyTenantCreated))]
            public void PublishTenantEvent<TEvent>(Guid tenantId, TEvent @event)
                where TEvent : IEvent
            {
            }
        }

        [AsyncApi]
        public class MixedOperationMethodPublisher
        {
            [Channel("mixed.operation.method", "mixed.operation.method")]
            [SendOperation(typeof(AnyTenantCreated), OperationId = "MethodSend")]
            [ReceiveOperation(typeof(AnyTenantUpdated), OperationId = "MethodReceive")]
            public void PublishOrReceive()
            {
            }
        }

        [AsyncApi]
        [Channel("custom.message.id", "custom.message.id")]
        [SendOperation(OperationId = "CustomMessageIdPublisher")]
        public class CustomMessageIdPublisher
        {
            [Message(typeof(AnyTenantCreated), MessageId = "tenant-created-event")]
            public void Publish()
            {
            }
        }

        [AsyncApi]
        public class OperationIdInferencePublisher
        {
            [Channel("tenant.created")]
            [SendOperation(typeof(AnyTenantCreated))]
            public void PublishTenantCreated()
            {
            }
        }

        [AsyncApi]
        public class ChannelIdInferencePublisher
        {
            [Channel("tenant.created")]
            [SendOperation(typeof(AnyTenantCreated))]
            public void Publish()
            {
            }
        }

        [AsyncApi]
        public class SignatureInferencePublisher
        {
            [Channel("tenant.signature.inferred")]
            [SendOperation]
            public void Publish(SignatureInferredPayload payload)
            {
            }
        }

        [AsyncApi]
        public class ConsumeContextInferenceConsumer
        {
            [Channel("tenant.consume.context")]
            [ReceiveOperation]
            public void Consume(ConsumeContext<ConsumeContextPayload> context)
            {
            }
        }

        [AsyncApi]
        public class RouteInferredPublisher
        {
            [Channel]
            [SendOperation(typeof(AnyTenantCreated))]
            [HttpPost("api/tenants/{tenantId}/created")]
            public void Publish([FromRoute] Guid tenantId, [FromBody] AnyTenantCreated payload)
            {
            }
        }

        [AsyncApi]
        public class ExplicitOverridesInferencePublisher
        {
            [Channel("customChannel", "tenant.created")]
            [SendOperation(typeof(AnyTenantCreated), OperationId = "customOperation")]
            [Message(typeof(AnyTenantCreated), MessageId = "customMessage", Title = "Custom title")]
            public void Publish(AnyTenantCreated payload)
            {
            }
        }

        [AsyncApi]
        public class ChannelTagsPublisher
        {
            [Channel("tenant.tags", "tenant.tags", Tags = new[] { "billing", "events" })]
            [SendOperation(typeof(AnyTenantCreated))]
            public void Publish()
            {
            }
        }

        [AsyncApi]
        public class RequestReplyPublisher
        {
            [Channel("orders.create", "orders.create")]
            [ReceiveOperation(typeof(CreateOrderRequest), OperationId = "CreateOrder", Reply = "orders.create.reply", ReplyMessagePayloadType = typeof(CreateOrderAccepted), ReplyAddressLocation = "$message.header#/replyTo")]
            public void Consume()
            {
            }
        }

        [AsyncApi("reply-without-address")]
        private class ReplyWithoutAddressPublisher
        {
            [Channel("orders.reply-without-address", "orders.reply-without-address")]
            [ReceiveOperation(typeof(CreateOrderRequest), OperationId = "ReplyWithoutAddress", Reply = "orders.reply-without-address.reply", ReplyMessagePayloadType = typeof(CreateOrderAccepted))]
            public void Consume()
            {
            }
        }

        [AsyncApi]
        private class ReplyMessageOverridePublisher
        {
            [Channel("orders.override", "orders.override")]
            [ReceiveOperation(typeof(CreateOrderRequest), OperationId = "ReplyMessageOverrides", Reply = "orders.override.reply", ReplyMessagePayloadType = typeof(CreateOrderAccepted), ReplyMessageId = "customReplyMessage", ReplyMessageName = "Contracts:CreateOrderAccepted", ReplyMessageTitle = "Create Order Accepted")]
            public void Consume()
            {
            }
        }

        [AsyncApi("static-reply-address")]
        private class ReplyChannelAddressPublisher
        {
            [Channel("orders.static-reply.request", "orders.static-reply.request")]
            [ReceiveOperation(typeof(CreateOrderRequest), OperationId = "StaticReplyAddress", Reply = "orders.static-reply", ReplyChannelAddress = "orders.static.reply.address", ReplyMessagePayloadType = typeof(CreateOrderAccepted))]
            public void Consume()
            {
            }
        }

        [AsyncApi("negative-reply-metadata")]
        private class InvalidReplyAddressPublisher
        {
            [Channel("orders.invalid-reply", "orders.invalid-reply")]
            [ReceiveOperation(typeof(CreateOrderRequest), ReplyAddressLocation = "$message.header#/replyTo")]
            public void Consume()
            {
            }
        }

        [AsyncApi("negative-reply-metadata")]
        private class ConflictingReplyMetadataPublisher
        {
            [Channel("orders.conflicting-reply", "orders.conflicting-reply")]
            [ReceiveOperation(typeof(CreateOrderRequest), Reply = "orders.conflicting-reply.reply", ReplyChannelAddress = "orders.conflicting-reply.reply", ReplyAddressLocation = "$message.header#/replyTo")]
            public void Consume()
            {
            }
        }

        private class ReplaceReplyMessageIdsOperationFilter : IOperationFilter
        {
            public void Apply(AsyncApiOperationDescriptor operation, OperationFilterContext context)
            {
                if (context.Member.Name != nameof(RequestReplyPublisher.Consume) || operation.Reply is null)
                {
                    return;
                }

                operation.Reply.MessageIds.Clear();
                operation.Reply.MessageIds.Add("createOrderRequest");
            }
        }

        private class ReplyChannelContextTaggingFilter : IChannelFilter
        {
            public void Apply(AsyncApiChannelDescriptor channel, ChannelFilterContext context)
            {
                if (context.Channel.ChannelId == "orders.create.reply")
                {
                    channel.Tags.Add(new AsyncApiTag { Name = "filtered-reply-channel" });
                }
            }
        }
    }

    public class AnyTenantCreated : IEvent { }

    public class AnyTenantUpdated : IEvent { }

    public class AnyTenantRemoved : IEvent { }

    public class SignatureInferredPayload
    {
        public string Id { get; set; } = string.Empty;
    }

    public class ConsumeContextPayload
    {
        public Guid Id { get; set; }
    }

    public class CreateOrderRequest
    {
        public Guid OrderId { get; set; }
    }

    public class CreateOrderAccepted
    {
        public Guid OrderId { get; set; }
    }

    public interface IEvent { }

    public interface ITenantMessagePublisher
    {
        void PublishTenantEvent<TEvent>(Guid tenantId, TEvent @event)
            where TEvent : IEvent;
    }
}
