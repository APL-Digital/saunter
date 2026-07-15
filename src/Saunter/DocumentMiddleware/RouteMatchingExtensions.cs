using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace Saunter.DocumentMiddleware
{
    internal static class RouteMatchingExtensions
    {
        // Route patterns are fixed for the app lifetime, so parse each pattern once.
        // TemplateMatcher is only read during TryMatch (writes land in the caller's
        // RouteValueDictionary), so a cached instance is safe to share across requests.
        private static readonly ConcurrentDictionary<string, TemplateMatcher> s_matchers = new();

        public static bool IsMatchingRoute(this PathString path, string pattern)
        {
            var matcher = s_matchers.GetOrAdd(pattern, static key =>
                new TemplateMatcher(TemplateParser.Parse(key), new RouteValueDictionary()));

            return matcher.TryMatch(path, new RouteValueDictionary());
        }

        public static bool TryGetDocument(this HttpContext context, [MaybeNullWhen(false)] out string document)
        {
            var values = context.Request.RouteValues;
            if (!values.TryGetValue("document", out var value) || value == null)
            {
                document = null;
                return false;
            }

            document = value.ToString() ?? string.Empty;
            return true;
        }
    }
}
