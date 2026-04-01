using System;
using ByteBard.AsyncAPI.Models;
using Saunter.AttributeProvider.Attributes;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider.DocumentGenerationTests
{
    public class ClassAttributesTests
    {
        [Theory]
        [InlineData(typeof(TenantMessageConsumer))]
        [InlineData(typeof(ITenantMessageConsumer))]
        public void GetDocument_GeneratesDocumentWithMultipleMessagesPerChannel(Type type)
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, type);

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var channel = document.AssertAndGetChannel("asw.tenant_service.tenants_history", "asw.tenant_service.tenants_history");
            document.AssertChannelMessages(channel, "tenantCreated", "tenantUpdated", "tenantRemoved");

            var receive = document.AssertAndGetOperation("TenantMessageConsumer", AsyncApiAction.Receive);
            document.AssertByMessage(receive, "tenantCreated", "tenantUpdated", "tenantRemoved");
        }

        [Theory]
        [InlineData(typeof(TenantMessagePublisher))]
        [InlineData(typeof(ITenantMessagePublisher))]
        public void GenerateDocument_GeneratesDocumentWithMultipleMessagesPerChannelInTheSameMethod(Type type)
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, type);

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var channel = document.AssertAndGetChannel("asw.tenant_service.tenants_history", "asw.tenant_service.tenants_history");
            document.AssertChannelMessages(channel, "tenantCreated", "tenantUpdated", "tenantRemoved");

            var send = document.AssertAndGetOperation("TenantMessagePublisher", AsyncApiAction.Send);
            document.AssertByMessage(send, "tenantCreated", "tenantUpdated", "tenantRemoved");
        }

        [Theory]
        [InlineData(typeof(TenantSingleMessagePublisher))]
        [InlineData(typeof(ITenantSingleMessagePublisher))]
        public void GenerateDocument_GeneratesDocumentWithSingleMessage(Type type)
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, type);

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var channel = document.AssertAndGetChannel("asw.tenant_service.tenants_history", "asw.tenant_service.tenants_history");
            document.AssertChannelMessages(channel, "anyTenantCreated");

            var send = document.AssertAndGetOperation("TenantSingleMessagePublisher", AsyncApiAction.Send);
            document.AssertByMessage(send, "anyTenantCreated");
        }

        [Theory]
        [InlineData(typeof(OneTenantMessageConsumer))]
        [InlineData(typeof(IOneTenantMessageConsumer))]
        public void GenerateDocument_GeneratesDocumentWithChannelParameters(Type type)
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, type);

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var channel = document.AssertAndGetChannel("asw.tenant_service.{tenant_id}.{tenant_status}", "asw.tenant_service.{tenant_id}.{tenant_status}");
            channel.Parameters.ShouldContainKey("tenant_id");
            channel.Parameters.ShouldContainKey("tenant_status");
            document.Components.Parameters.ShouldContainKey("tenant_id");
            document.Components.Parameters.ShouldContainKey("tenant_status");

            var receive = document.AssertAndGetOperation("OneTenantMessageConsumer", AsyncApiAction.Receive);
            document.AssertByMessage(receive, "tenantCreated", "tenantUpdated", "tenantRemoved");
        }

        [Fact]
        public void GenerateDocument_ResolvesMessagesPerOperationOnType()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(MixedOperationTypePublisher));

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var channel = document.AssertAndGetChannel("mixed.operation.type", "mixed.operation.type");
            document.AssertChannelMessages(channel, "anyTenantCreated", "anyTenantUpdated");

            var send = document.AssertAndGetOperation("TypeSend", AsyncApiAction.Send);
            document.AssertByMessage(send, "anyTenantCreated");

            var receive = document.AssertAndGetOperation("TypeReceive", AsyncApiAction.Receive);
            document.AssertByMessage(receive, "anyTenantUpdated");
        }

        [AsyncApi]
        [Channel("channel.my.message", "channel.my.message")]
        [SendOperation]
        public class MyMessagePublisher
        {
            [Message(typeof(MyMessage), HeadersType = typeof(MyMessageHeader))]
            public void PublishMyMessage() { }
        }

        [AsyncApi]
        [Channel("channel.my.message.interface", "channel.my.message.interface")]
        [SendOperation]
        public interface IMyMessagePublisher
        {
            [Message(typeof(MyMessage), HeadersType = typeof(MyMessageHeader))]
            void PublishMyMessage();
        }

        [AsyncApi]
        [Channel("asw.tenant_service.tenants_history", "asw.tenant_service.tenants_history", Description = "Tenant events.")]
        [ReceiveOperation(OperationId = "TenantMessageConsumer", Summary = "Receive domains events about tenants.")]
        public class TenantMessageConsumer
        {
            [Message(typeof(TenantCreated))]
            public void ReceiveTenantCreatedEvent(Guid id, TenantCreated created) { }

            [Message(typeof(TenantUpdated))]
            public void ReceiveTenantUpdatedEvent(Guid id, TenantUpdated updated) { }

            [Message(typeof(TenantRemoved))]
            public void ReceiveTenantRemovedEvent(Guid id, TenantRemoved removed) { }
        }

        [AsyncApi]
        [Channel("asw.tenant_service.tenants_history", "asw.tenant_service.tenants_history", Description = "Tenant events.")]
        [ReceiveOperation(OperationId = "TenantMessageConsumer", Summary = "Receive domains events about tenants.")]
        public interface ITenantMessageConsumer
        {
            [Message(typeof(TenantCreated))]
            void ReceiveTenantCreatedEvent(Guid _, TenantCreated __);

            [Message(typeof(TenantUpdated))]
            void ReceiveTenantUpdatedEvent(Guid _, TenantUpdated __);

            [Message(typeof(TenantRemoved))]
            void ReceiveTenantRemovedEvent(Guid _, TenantRemoved __);
        }

        [AsyncApi]
        [Channel("asw.tenant_service.tenants_history", "asw.tenant_service.tenants_history", Description = "Tenant events.")]
        [SendOperation(OperationId = "TenantMessagePublisher", Summary = "Send domains events about tenants.")]
        public class TenantMessagePublisher
        {
            [Message(typeof(TenantCreated))]
            public void PublishTenantCreatedEvent(Guid id, TenantCreated created) { }

            [Message(typeof(TenantUpdated))]
            public void PublishTenantUpdatedEvent(Guid id, TenantUpdated updated) { }

            [Message(typeof(TenantRemoved))]
            public void PublishTenantRemovedEvent(Guid id, TenantRemoved removed) { }
        }

        [AsyncApi]
        [Channel("asw.tenant_service.tenants_history", "asw.tenant_service.tenants_history", Description = "Tenant events.")]
        [SendOperation(OperationId = "TenantMessagePublisher", Summary = "Send domains events about tenants.")]
        public interface ITenantMessagePublisher
        {
            [Message(typeof(TenantCreated))]
            void PublishTenantCreatedEvent(Guid _, TenantCreated __);

            [Message(typeof(TenantUpdated))]
            void PublishTenantUpdatedEvent(Guid _, TenantUpdated __);

            [Message(typeof(TenantRemoved))]
            void PublishTenantRemovedEvent(Guid _, TenantRemoved __);
        }

        [AsyncApi]
        [Channel("asw.tenant_service.tenants_history", "asw.tenant_service.tenants_history", Description = "Tenant events.")]
        [SendOperation(OperationId = "TenantSingleMessagePublisher", Summary = "Send single domain event about tenants.")]
        public class TenantSingleMessagePublisher
        {
            [Message(typeof(AnyTenantCreated))]
            public void PublishTenantCreated(Guid id, AnyTenantCreated created)
            {
            }
        }

        [AsyncApi]
        [Channel("asw.tenant_service.tenants_history", "asw.tenant_service.tenants_history", Description = "Tenant events.")]
        [SendOperation(OperationId = "TenantSingleMessagePublisher", Summary = "Send single domain event about tenants.")]
        public interface ITenantSingleMessagePublisher
        {
            [Message(typeof(AnyTenantCreated))]
            public void PublishTenantCreated(Guid _, AnyTenantCreated __);
        }

        [AsyncApi]
        [Channel("asw.tenant_service.{tenant_id}.{tenant_status}", "asw.tenant_service.{tenant_id}.{tenant_status}", Description = "A tenant events.")]
        [ChannelParameter("tenant_id", typeof(long), Description = "The tenant identifier.")]
        [ChannelParameter("tenant_status", typeof(string), Description = "The tenant status.")]
        [ReceiveOperation(OperationId = "OneTenantMessageConsumer", Summary = "Receive domains events about a tenant.")]
        public class OneTenantMessageConsumer
        {
            [Message(typeof(TenantCreated))]
            public void ReceiveTenantCreatedEvent(Guid id, TenantCreated created) { }

            [Message(typeof(TenantUpdated))]
            public void ReceiveTenantUpdatedEvent(Guid id, TenantUpdated updated) { }

            [Message(typeof(TenantRemoved))]
            public void ReceiveTenantRemovedEvent(Guid id, TenantRemoved removed) { }
        }

        [AsyncApi]
        [Channel("asw.tenant_service.{tenant_id}.{tenant_status}", "asw.tenant_service.{tenant_id}.{tenant_status}", Description = "A tenant events.")]
        [ChannelParameter("tenant_id", typeof(long), Description = "The tenant identifier.")]
        [ChannelParameter("tenant_status", typeof(string), Description = "The tenant status.")]
        [ReceiveOperation(OperationId = "OneTenantMessageConsumer", Summary = "Receive domains events about a tenant.")]
        public interface IOneTenantMessageConsumer
        {
            [Message(typeof(TenantCreated))]
            void ReceiveTenantCreatedEvent(Guid _, TenantCreated __);

            [Message(typeof(TenantUpdated))]
            void ReceiveTenantUpdatedEvent(Guid _, TenantUpdated __);

            [Message(typeof(TenantRemoved))]
            void ReceiveTenantRemovedEvent(Guid _, TenantRemoved __);
        }

        [AsyncApi]
        [Channel("mixed.operation.type", "mixed.operation.type")]
        [SendOperation(typeof(AnyTenantCreated), OperationId = "TypeSend")]
        [ReceiveOperation(typeof(AnyTenantUpdated), OperationId = "TypeReceive")]
        public class MixedOperationTypePublisher
        {
            public void PublishOrReceive()
            {
            }
        }
    }

    public class TenantCreated { }

    public class TenantUpdated { }

    public class TenantRemoved { }

    public class MyMessage { }

    public class MyMessageHeader
    {
        public string StringHeader { get; set; }
        public int? NullableIntHeader { get; set; }
    }
}
