using System;
using System.Text;
using Saunter.AttributeProvider;

namespace MassTransitUseCases.Resolvers;

public sealed class TenantCatalogChannelResolver : IChannelResolver
{
    private readonly Type _messageType;

    public TenantCatalogChannelResolver(Type messageType)
    {
        _messageType = messageType;
    }

    public string ResolveChannelName()
    {
        return $"tenants/catalog/{ToKebabCase(_messageType.Name)}";
    }

    private static string ToKebabCase(string value)
    {
        var builder = new StringBuilder();

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && index > 0)
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
