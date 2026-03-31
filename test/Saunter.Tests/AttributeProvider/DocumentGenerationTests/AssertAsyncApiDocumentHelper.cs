using System.Linq;
using ByteBard.AsyncAPI.Models;
using Saunter.AttributeProvider.Descriptors;
using Shouldly;

namespace Saunter.Tests.AttributeProvider.DocumentGenerationTests
{
    internal static class AssertAsyncApiDocumentHelper
    {
        public static AsyncApiChannelDescriptor AssertAndGetChannel(this AsyncApiDocumentDescriptor document, string key, string address)
        {
            document.Channels.Count.ShouldBeGreaterThanOrEqualTo(1);
            document.Channels.ShouldContainKey(key);

            var channel = document.Channels[key];
            channel.ShouldNotBeNull();
            channel.Address.ShouldBe(address);

            return channel;
        }

        public static AsyncApiOperationDescriptor AssertAndGetOperation(this AsyncApiDocumentDescriptor document, string key, AsyncApiAction action)
        {
            document.Operations.ShouldContainKey(key);

            var operation = document.Operations[key];
            operation.ShouldNotBeNull();
            operation.Action.ShouldBe(action);

            return operation;
        }

        public static void AssertByMessage(this AsyncApiDocumentDescriptor document, AsyncApiOperationDescriptor operation, params string[] messageIds)
        {
            operation.MessageIds.Count.ShouldBe(messageIds.Length);

            foreach (var messageId in messageIds)
            {
                operation.MessageIds.ShouldContain(messageId);
                document.Components.Messages.ShouldContainKey(messageId);
                document.Components.Messages[messageId].PayloadSchemaId.ShouldNotBeNull();
                document.Components.Schemas.ShouldContainKey(document.Components.Messages[messageId].PayloadSchemaId!);
            }
        }

        public static void AssertChannelMessages(this AsyncApiDocumentDescriptor document, AsyncApiChannelDescriptor channel, params string[] messageIds)
        {
            channel.MessageIds.Count.ShouldBe(messageIds.Length);

            foreach (var messageId in messageIds)
            {
                channel.MessageIds.ShouldContain(messageId);
            }
        }
    }
}
