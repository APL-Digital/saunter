using System;
using System.Collections.Generic;
using System.Linq;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;

namespace Saunter.AttributeProvider
{
    internal static class AttributeProviderModelFactory
    {
        public static string GetReferenceKey(string reference)
        {
            ArgumentNullException.ThrowIfNull(reference);
            var segments = reference.Split('/');
            return segments[^1];
        }

        public static string SanitizeComponentKey(string key)
        {
            var sanitized = new string(key
                .Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_' ? ch : '_')
                .ToArray());

            return string.IsNullOrWhiteSpace(sanitized) ? "component" : sanitized;
        }

        public static AsyncApiBindings<TBinding> CreateBindingsReference<TBinding>(string? bindingsRef, string componentName)
            where TBinding : IBinding
        {
            if (string.IsNullOrWhiteSpace(bindingsRef))
            {
                return new AsyncApiBindings<TBinding>();
            }

            return new AsyncApiBindingsReference<TBinding>($"#/components/{componentName}/{bindingsRef}");
        }

        public static AsyncApiMultiFormatSchema? CreateSchemaWrapper(AsyncApiJsonSchema? schema)
        {
            if (schema is null)
            {
                return null;
            }

            return new AsyncApiMultiFormatSchema
            {
                Schema = schema,
            };
        }

        public static AsyncApiExternalDocumentation? CreateExternalDocs(string? url, string? description)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException($"ExternalDocs must be a valid absolute URI. Value: '{url}'.");
            }

            return new AsyncApiExternalDocumentation
            {
                Url = uri,
                Description = description,
            };
        }
    }
}
