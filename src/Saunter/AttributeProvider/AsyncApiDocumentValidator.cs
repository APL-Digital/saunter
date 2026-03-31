using System;

namespace Saunter.AttributeProvider
{
    internal class AsyncApiDocumentValidator : IAsyncApiDocumentValidator
    {
        public void Validate(AsyncApiDocumentDescriptor document)
        {
            foreach (var channel in document.Channels.Values)
            {
                foreach (var serverName in channel.ServerNames)
                {
                    if (!document.Servers.ContainsKey(serverName))
                    {
                        throw new InvalidOperationException($"Channel references unknown server '{serverName}'.");
                    }
                }
            }

            foreach (var operation in document.Operations.Values)
            {
                if (operation.Reply?.ChannelId is null)
                {
                    continue;
                }

                if (!document.Channels.ContainsKey(operation.Reply.ChannelId))
                {
                    throw new InvalidOperationException($"Operation reply references unknown channel '{operation.Reply.ChannelId}'.");
                }
            }
        }
    }
}
