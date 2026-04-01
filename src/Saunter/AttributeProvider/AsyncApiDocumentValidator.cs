using System;

namespace Saunter.AttributeProvider
{
    internal class AsyncApiDocumentValidator : IAsyncApiDocumentValidator
    {
        public void Validate(AsyncApiDocumentDescriptor document)
        {
            foreach (var channel in document.Channels.Values)
            {
                foreach (var messageId in channel.Messages.Values)
                {
                    if (!document.Components.Messages.ContainsKey(messageId))
                    {
                        throw new InvalidOperationException($"Channel '{channel.Id}' references unknown message '{messageId}'. Add it to components/messages or remove it from the channel messages list.");
                    }
                }

                foreach (var serverName in channel.ServerNames)
                {
                    if (!document.Servers.ContainsKey(serverName))
                    {
                        throw new InvalidOperationException($"Channel '{channel.Id}' references unknown server '{serverName}'. Add the server to document.Servers or remove it from the channel Servers list.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(channel.BindingsRef) && !document.Components.ChannelBindings.ContainsKey(channel.BindingsRef))
                {
                    throw new InvalidOperationException($"Channel '{channel.Id}' references unknown channel binding '{channel.BindingsRef}'. Add it to components/channelBindings or remove the BindingsRef.");
                }
            }

            foreach (var message in document.Components.Messages.Values)
            {
                if (!string.IsNullOrWhiteSpace(message.CorrelationIdRef) && !document.Components.CorrelationIds.ContainsKey(message.CorrelationIdRef))
                {
                    throw new InvalidOperationException($"Message '{message.Id}' references unknown correlation id '{message.CorrelationIdRef}'. Add it to components/correlationIds or remove the CorrelationId reference.");
                }

                if (!string.IsNullOrWhiteSpace(message.BindingsRef) && !document.Components.MessageBindings.ContainsKey(message.BindingsRef))
                {
                    throw new InvalidOperationException($"Message '{message.Id}' references unknown message binding '{message.BindingsRef}'. Add it to components/messageBindings or remove the BindingsRef.");
                }
            }

            foreach (var operationPair in document.Operations)
            {
                var operationId = operationPair.Key;
                var operation = operationPair.Value;

                if (!document.Channels.ContainsKey(operation.ChannelId))
                {
                    throw new InvalidOperationException($"Operation '{operationId}' references unknown channel '{operation.ChannelId}'. Add the channel or correct the inferred/explicit channel id.");
                }

                var channel = document.Channels[operation.ChannelId];
                foreach (var messageId in operation.MessageIds)
                {
                    if (!channel.Messages.ContainsKey(messageId))
                    {
                        throw new InvalidOperationException($"Operation '{operationId}' references unknown channel message '{messageId}' on channel '{operation.ChannelId}'. Add it to the channel messages list or remove it from the operation.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(operation.BindingsRef) && !document.Components.OperationBindings.ContainsKey(operation.BindingsRef))
                {
                    throw new InvalidOperationException($"Operation '{operationId}' references unknown operation binding '{operation.BindingsRef}'. Add it to components/operationBindings or remove the BindingsRef.");
                }

                foreach (var traitReference in operation.TraitReferences)
                {
                    if (!document.Components.OperationTraits.ContainsKey(traitReference))
                    {
                        throw new InvalidOperationException($"Operation '{operationId}' references unknown operation trait '{traitReference}'. Add it to components/operationTraits or remove the trait reference.");
                    }
                }

                if (operation.Reply?.ChannelId is null)
                {
                    continue;
                }

                if (!document.Channels.ContainsKey(operation.Reply.ChannelId))
                {
                    throw new InvalidOperationException($"Operation '{operationId}' reply references unknown channel '{operation.Reply.ChannelId}'. Add the reply channel or remove the Reply reference.");
                }
            }
        }
    }
}
