using System.Linq;
using ByteBard.AsyncAPI.Models;
using Shouldly;

namespace Saunter.Tests.AttributeProvider.DocumentGenerationTests
{
    internal static class AssertAsyncApiDocumentHelper
    {
        public static AsyncApiChannel AssertAndGetChannel(this AsyncApiDocument document, string key, string address)
        {
            document.Channels.Count.ShouldBeGreaterThanOrEqualTo(1);
            document.Channels.ShouldContainKey(key);

            var channel = document.Channels[key];
            channel.ShouldNotBeNull();
            channel.Address.ShouldBe(address);

            return channel;
        }

        public static AsyncApiOperation AssertAndGetOperation(this AsyncApiDocument document, string key, AsyncApiAction action)
        {
            document.Operations.ShouldContainKey(key);

            var operation = document.Operations[key];
            operation.ShouldNotBeNull();
            operation.Action.ShouldBe(action);

            return operation;
        }

        public static void AssertByMessage(this AsyncApiDocument document, AsyncApiOperation operation, params string[] messageIds)
        {
            operation.Messages.Count.ShouldBe(messageIds.Length);
            operation.Messages.ShouldAllBe(message => message.Reference != null);

            foreach (var messageId in messageIds)
            {
                operation.Messages.ShouldContain(message =>
                    message.Reference.Reference.Contains("/channels/")
                    && message.Reference.Reference.EndsWith($"/{messageId}"));
                document.Components.Messages.ShouldContainKey(messageId);
                document.Components.Schemas.ShouldContainKey(messageId);
            }
        }

        public static void AssertChannelMessages(this AsyncApiDocument document, AsyncApiChannel channel, params string[] messageIds)
        {
            channel.Messages.Count.ShouldBe(messageIds.Length);

            foreach (var messageId in messageIds)
            {
                channel.Messages.ShouldContainKey(messageId);
                channel.Messages[messageId].ShouldBeOfType<AsyncApiMessageReference>();
            }
        }
    }
}
