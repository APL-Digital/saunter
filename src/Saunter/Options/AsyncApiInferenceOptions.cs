using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Saunter.Options
{
    /// <summary>
    /// Controls how Saunter infers values that were not specified explicitly on attributes,
    /// such as operation ids, channel ids/addresses, payload types and message names.
    /// </summary>
    public class AsyncApiInferenceOptions
    {
        /// <summary>
        /// When <c>true</c> (the default) and no <c>OperationId</c> is set on the operation attribute,
        /// the operation id is produced by <see cref="OperationIdGenerator"/>. When <c>false</c>, a
        /// fallback of the form <c>DeclaringType.MemberName.action</c> is used instead.
        /// </summary>
        public bool InferOperationIdFromMemberName { get; set; } = true;

        /// <summary>
        /// When <c>true</c> (the default) and no channel id is set on the channel attribute,
        /// the channel id is produced by passing the channel address to <see cref="ChannelIdGenerator"/>.
        /// When <c>false</c>, omitting the channel id throws during generation.
        /// </summary>
        public bool InferChannelIdFromAddress { get; set; } = true;

        /// <summary>
        /// When <c>true</c> (the default) and a channel method has no <c>[Message]</c> attributes and the
        /// operation attribute specifies no payload type, message payload types are inferred from the
        /// method's parameter and return types.
        /// </summary>
        public bool InferPayloadTypeFromMethodSignature { get; set; } = true;

        /// <summary>
        /// When <c>true</c> (the default) and no address is set on the channel attribute, the channel
        /// address is taken from ASP.NET Core route metadata (e.g. <c>[Route]</c>/<c>[HttpGet]</c> templates)
        /// on the member. When <c>false</c>, omitting the address throws during generation.
        /// </summary>
        public bool InferChannelAddressFromRoute { get; set; } = true;

        /// <summary>
        /// When <c>true</c> (the default), a document without an explicit <c>defaultContentType</c>
        /// is given <c>application/json</c>.
        /// </summary>
        public bool AutoSetDefaultContentType { get; set; } = true;

        /// <summary>
        /// Produces an operation id from the annotated member and the operation action (send/receive).
        /// Used when <see cref="InferOperationIdFromMemberName"/> is enabled and the attribute sets no
        /// explicit id. The default returns the member name and ignores the action.
        /// </summary>
        public Func<MemberInfo, ByteBard.AsyncAPI.Models.AsyncApiAction, string> OperationIdGenerator { get; set; } = (member, _) => member.Name;

        /// <summary>
        /// Produces a channel id from a channel address. Used when <see cref="InferChannelIdFromAddress"/>
        /// is enabled and the attribute sets no explicit id; the result is sanitized to a valid component key.
        /// The default strips <c>{parameter}</c> segments, splits the address on separators
        /// (<c>. / { } - _</c>), takes the last two tokens containing letters and camel-cases them
        /// (e.g. <c>orders/eu/created.{id}</c> becomes <c>euCreated</c>), falling back to <c>"channel"</c>
        /// when no tokens remain.
        /// </summary>
        public Func<string, string> ChannelIdGenerator { get; set; } = address =>
        {
            var addressWithoutParameters = Regex.Replace(address, @"\{[^}]+\}", string.Empty);
            var tokens = addressWithoutParameters
                .Split(new[] { '.', '/', '{', '}', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(token => token.Any(char.IsLetter))
                .ToArray();

            if (tokens.Length == 0)
            {
                return "channel";
            }

            var selectedTokens = tokens.Skip(Math.Max(0, tokens.Length - 2)).ToArray();
            var head = selectedTokens[0].ToLowerInvariant();
            var tail = string.Concat(selectedTokens.Skip(1).Select(ToPascalCase));
            return head + tail;
        };

        /// <summary>
        /// Produces a machine-friendly message name from the payload type when no explicit name is set.
        /// The default camel-cases the type name (e.g. <c>OrderCreatedEvent</c> becomes <c>orderCreatedEvent</c>).
        /// </summary>
        public Func<Type, string> MessageNameGenerator { get; set; } = type => ToCamelCase(type.Name);

        /// <summary>
        /// Produces a human-friendly message title from the payload type when no explicit title is set.
        /// The default splits the type name into words (e.g. <c>OrderCreatedEvent</c> becomes <c>Order Created Event</c>).
        /// </summary>
        public Func<Type, string> MessageTitleGenerator { get; set; } = type => string.Join(" ", SplitWords(type.Name));

        internal static string ToPascalCase(string value)
        {
            var words = SplitWords(value).ToArray();
            return string.Concat(words.Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
        }

        internal static string ToCamelCase(string value)
        {
            var pascal = ToPascalCase(value);
            return pascal.Length == 0 ? "message" : char.ToLowerInvariant(pascal[0]) + pascal[1..];
        }

        private static IEnumerable<string> SplitWords(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield break;
            }

            var word = new List<char>();
            for (var i = 0; i < value.Length; i++)
            {
                var current = value[i];
                if (!char.IsLetterOrDigit(current))
                {
                    if (word.Count > 0)
                    {
                        yield return new string(word.ToArray());
                        word.Clear();
                    }

                    continue;
                }

                var isBoundary = word.Count > 0
                    && char.IsUpper(current)
                    && (char.IsLower(word[^1]) || (i + 1 < value.Length && char.IsLower(value[i + 1])));
                if (isBoundary)
                {
                    yield return new string(word.ToArray());
                    word.Clear();
                }

                word.Add(current);
            }

            if (word.Count > 0)
            {
                yield return new string(word.ToArray());
            }
        }
    }
}
