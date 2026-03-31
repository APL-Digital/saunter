using System;
using ByteBard.AsyncAPI.Bindings.Kafka;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;
using Saunter.AttributeProvider.Attributes;
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
    }

    public class AnyTenantCreated : IEvent { }

    public class AnyTenantUpdated : IEvent { }

    public class AnyTenantRemoved : IEvent { }

    public interface IEvent { }

    public interface ITenantMessagePublisher
    {
        void PublishTenantEvent<TEvent>(Guid tenantId, TEvent @event)
            where TEvent : IEvent;
    }
}
