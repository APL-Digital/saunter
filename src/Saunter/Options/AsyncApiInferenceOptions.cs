using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Saunter.Options
{
    public class AsyncApiInferenceOptions
    {
        public bool InferOperationIdFromMemberName { get; set; } = true;

        public bool InferChannelIdFromAddress { get; set; } = true;

        public bool InferPayloadTypeFromMethodSignature { get; set; } = true;

        public bool InferChannelAddressFromRoute { get; set; } = true;

        public bool AutoSetDefaultContentType { get; set; } = true;

        public Func<MemberInfo, ByteBard.AsyncAPI.Models.AsyncApiAction, string> OperationIdGenerator { get; set; } = (member, _) => member.Name;

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

        public Func<Type, string> MessageNameGenerator { get; set; } = type => ToCamelCase(type.Name);

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
