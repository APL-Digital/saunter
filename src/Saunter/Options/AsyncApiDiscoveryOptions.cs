using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Saunter.Options
{
    /// <summary>
    /// Controls convention-based discovery of channels and operations that are not
    /// declared with attributes.
    /// </summary>
    public class AsyncApiDiscoveryOptions
    {
        /// <summary>
        /// When <c>true</c>, concrete classes implementing MassTransit's <c>IConsumer&lt;T&gt;</c>
        /// found in the scanned assemblies are documented with a receive operation per consumed
        /// message type, without requiring <c>[AsyncApi]</c>/<c>[Channel]</c>/<c>[ReceiveOperation]</c>
        /// attributes. Types that already carry AsyncAPI attributes are skipped, so annotated
        /// consumers keep their hand-authored documentation. Only the consume side is discoverable;
        /// publishes remain attribute-driven. Default <c>false</c>.
        /// </summary>
        public bool DiscoverMassTransitConsumers { get; set; }

        /// <summary>
        /// Produces the channel address for a discovered consumed message type. Defaults to
        /// MassTransit's message topology naming, the message type's <c>Namespace:TypeName</c>
        /// (its MessageUrn), which is the exchange/topic a published message targets.
        /// Use <see cref="KebabCaseEndpointAddress"/> for queue-oriented channel naming instead.
        /// </summary>
        public Func<Type, string> MassTransitChannelAddressGenerator { get; set; } = MessageUrnAddress;

        /// <summary>
        /// Optional predicate to exclude discovered consumer types. When <c>null</c>, every
        /// discovered consumer is documented.
        /// </summary>
        public Func<TypeInfo, bool>? MassTransitConsumerFilter { get; set; }

        /// <summary>
        /// The default address generator: the message type's <c>Namespace:TypeName</c>, matching
        /// MassTransit's message topology (MessageUrn) naming.
        /// </summary>
        public static string MessageUrnAddress(Type messageType)
        {
            ArgumentNullException.ThrowIfNull(messageType);
            return string.IsNullOrEmpty(messageType.Namespace)
                ? messageType.Name
                : $"{messageType.Namespace}:{messageType.Name}";
        }

        /// <summary>
        /// An alternative address generator producing the kebab-cased message type name
        /// (e.g. <c>OrderSubmitted</c> becomes <c>order-submitted</c>), matching MassTransit's
        /// <c>KebabCaseEndpointNameFormatter</c> queue naming style.
        /// </summary>
        public static string KebabCaseEndpointAddress(Type messageType)
        {
            ArgumentNullException.ThrowIfNull(messageType);

            var name = messageType.Name;
            var backtick = name.IndexOf('`');
            if (backtick >= 0)
            {
                name = name.Substring(0, backtick);
            }

            var builder = new StringBuilder(name.Length + 8);
            for (var i = 0; i < name.Length; i++)
            {
                var current = name[i];
                if (char.IsUpper(current))
                {
                    if (i > 0 && (!char.IsUpper(name[i - 1]) || (i + 1 < name.Length && !char.IsUpper(name[i + 1]))))
                    {
                        builder.Append('-');
                    }

                    builder.Append(char.ToLowerInvariant(current));
                }
                else
                {
                    builder.Append(current);
                }
            }

            return builder.ToString();
        }
    }
}
