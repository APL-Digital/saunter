using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Saunter.AttributeProvider.Attributes;
using Saunter.Options;

namespace Saunter.AttributeProvider
{
    /// <summary>
    /// Discovers MassTransit <c>IConsumer&lt;T&gt;</c> implementations by convention and synthesizes
    /// the channel and receive-operation attributes the regular attribute pipeline consumes.
    /// MassTransit types are matched by name so the library takes no MassTransit dependency.
    /// </summary>
    internal static class MassTransitConsumerDiscovery
    {
        private const string ConsumerInterfaceFullName = "MassTransit.IConsumer`1";
        private const string ConsumeContextFullName = "MassTransit.ConsumeContext`1";

        internal readonly record struct DiscoveredConsumerOperation(
            MethodInfo Method,
            ChannelAttribute Channel,
            OperationAttribute Operation);

        /// <summary>
        /// Finds concrete, unannotated consumer classes among <paramref name="sourceTypes"/> and
        /// yields one synthesized channel/receive-operation pair per consumed message type.
        /// Types carrying any AsyncAPI attribute are skipped: annotation always wins over convention.
        /// </summary>
        public static IEnumerable<DiscoveredConsumerOperation> Discover(
            IEnumerable<TypeInfo> sourceTypes,
            AsyncApiDiscoveryOptions discoveryOptions)
        {
            foreach (var type in sourceTypes)
            {
                if (type.IsAbstract || type.IsInterface || !type.IsClass)
                {
                    continue;
                }

                if (IsAnnotated(type))
                {
                    continue;
                }

                if (discoveryOptions.MassTransitConsumerFilter?.Invoke(type) == false)
                {
                    continue;
                }

                var messageTypes = type.ImplementedInterfaces
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition().FullName == ConsumerInterfaceFullName)
                    .Select(i => i.GenericTypeArguments[0])
                    .Distinct()
                    .ToArray();

                foreach (var messageType in messageTypes)
                {
                    var consumeMethod = FindConsumeMethod(type, messageType);
                    if (consumeMethod is null)
                    {
                        continue;
                    }

                    var address = discoveryOptions.MassTransitChannelAddressGenerator(messageType);
                    var channel = new ChannelAttribute(address);
                    var operation = new ReceiveOperationAttribute
                    {
                        OperationId = $"{type.Name}.{messageType.Name}.receive",
                    };

                    yield return new DiscoveredConsumerOperation(consumeMethod, channel, operation);
                }
            }
        }

        private static bool IsAnnotated(TypeInfo type)
        {
            // Annotations may live on the class itself, on its methods, on a base class, or on an
            // implemented interface (interface-based annotation is a supported authoring pattern) —
            // any of them means the type is documented through the attribute pipeline already.
            return SelfAndInterfaces(type).Any(HasAsyncApiAttributes);
        }

        private static IEnumerable<TypeInfo> SelfAndInterfaces(TypeInfo type)
        {
            for (var current = type; current is not null && current.AsType() != typeof(object); current = current.BaseType?.GetTypeInfo())
            {
                yield return current;
            }

            foreach (var implementedInterface in type.ImplementedInterfaces)
            {
                yield return implementedInterface.GetTypeInfo();
            }
        }

        private static bool HasAsyncApiAttributes(TypeInfo type)
        {
            if (type.GetCustomAttribute<AsyncApiAttribute>() is not null
                || type.GetCustomAttribute<ChannelAttribute>() is not null
                || type.GetCustomAttribute<OperationAttribute>() is not null)
            {
                return true;
            }

            return type.DeclaredMethods.Any(method =>
                method.GetCustomAttribute<ChannelAttribute>() is not null
                || method.GetCustomAttribute<OperationAttribute>() is not null);
        }

        private static MethodInfo? FindConsumeMethod(TypeInfo type, Type messageType)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(method =>
                    method.Name == "Consume"
                    && method.GetParameters() is { Length: 1 } parameters
                    && parameters[0].ParameterType.IsGenericType
                    && parameters[0].ParameterType.GetGenericTypeDefinition().FullName == ConsumeContextFullName
                    && parameters[0].ParameterType.GenericTypeArguments[0] == messageType);
        }
    }
}
