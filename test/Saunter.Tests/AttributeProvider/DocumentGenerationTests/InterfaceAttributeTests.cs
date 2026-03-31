using System;
using ByteBard.AsyncAPI.Models;
using Saunter.AttributeProvider.Attributes;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider.DocumentGenerationTests
{
    public class InterfaceAttributeTests
    {
        [Theory]
        [InlineData(typeof(IServiceEvents))]
        [InlineData(typeof(ServiceEventsFromInterface))]
        [InlineData(typeof(ServiceEventsFromAnnotatedInterface))]
        public void NonAnnotatedTypesTest(Type type)
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, type);

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            document.Channels.Count.ShouldBe(0);
            document.Operations.Count.ShouldBe(0);
        }

        [Theory]
        [InlineData(typeof(IAnnotatedServiceEvents), "interface.event.service.annotated.interface")]
        [InlineData(typeof(AnnotatedServiceEventsFromAnnotatedInterface), "class.event.service.annotated.interface")]
        [InlineData(typeof(SecondAnnotatedServiceEventsFromAnnotatedInterface), "class.event.secondservice.annotated.interface")]
        public void AnnotatedTypesTest(Type type, string channelAddress)
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, type);

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var channel = document.AssertAndGetChannel(channelAddress, channelAddress);
            var send = document.AssertAndGetOperation("PublishEvent", AsyncApiAction.Send);

            document.AssertByMessage(send, "tenantEvent");
            channel.ShouldNotBeNull();
        }

        [AsyncApi]
        private interface IAnnotatedServiceEvents
        {
            [Channel("interface.event.service.annotated.interface", "interface.event.service.annotated.interface")]
            [SendOperation(typeof(TenantEvent), OperationId = "PublishEvent", Description = "(interface.event.service.annotated.interface) Send domains events about a tenant.")]
            void PublishEvent(TenantEvent evt);
        }

        private interface IServiceEvents
        {
            void PublishEvent(TenantEvent evt);
        }

        private class ServiceEventsFromInterface : IServiceEvents
        {
            public void PublishEvent(TenantEvent evt) { }
        }

        private class ServiceEventsFromAnnotatedInterface : IAnnotatedServiceEvents
        {
            public void PublishEvent(TenantEvent evt) { }
        }

        [AsyncApi]
        private class AnnotatedServiceEventsFromAnnotatedInterface : IAnnotatedServiceEvents
        {
            [Channel("class.event.service.annotated.interface", "class.event.service.annotated.interface")]
            [SendOperation(typeof(TenantEvent), OperationId = "PublishEvent", Description = "(class.event.service.annotated.interface) Send domains events about a tenant.")]
            public void PublishEvent(TenantEvent evt) { }
        }

        [AsyncApi]
        private class SecondAnnotatedServiceEventsFromAnnotatedInterface : IAnnotatedServiceEvents
        {
            [Channel("class.event.secondservice.annotated.interface", "class.event.secondservice.annotated.interface")]
            [SendOperation(typeof(TenantEvent), OperationId = "PublishEvent", Description = "(class.event.secondservice.annotated.interface) Send domains events about a tenant.")]
            public void PublishEvent(TenantEvent evt) { }
        }

        private class TenantEvent { }
    }
}
