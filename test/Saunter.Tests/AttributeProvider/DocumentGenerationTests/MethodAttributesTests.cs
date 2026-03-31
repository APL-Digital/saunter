using System;
using ByteBard.AsyncAPI.Bindings.Kafka;
using ByteBard.AsyncAPI.Models;
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
                    ["sample_kaffka"] = new()
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

            send.Bindings.Reference.Reference.ShouldBe("#/components/operationBindings/sample_kaffka");
            document.AssertByMessage(send, "anyTenantCreated");
            document.Components.OperationBindings.ShouldContainKey("sample_kaffka");
            document.Components.OperationBindings["sample_kaffka"]["kafka"].ShouldBeOfType<KafkaOperationBinding>();
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
        [SendOperation(OperationId = "TenantMessagePublisher", Summary = "Send domains events about tenants.", BindingsRef = "sample_kaffka")]
        public class TenantMessagePublisherWithBind : ITenantMessagePublisher
        {
            [Message(typeof(AnyTenantCreated))]
            public void PublishTenantEvent<TEvent>(Guid tenantId, TEvent @event)
                where TEvent : IEvent
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
