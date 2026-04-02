using System;
using System.Linq;
using ByteBard.AsyncAPI;
using ByteBard.AsyncAPI.Models;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.SharedKernel
{
    internal class AsyncApiDocumentWriter : IAsyncApiDocumentWriter
    {
        private readonly IAsyncApiDocumentMapper _documentMapper;

        public AsyncApiDocumentWriter(IAsyncApiDocumentMapper documentMapper)
        {
            _documentMapper = documentMapper;
        }

        public string WriteJson(AsyncApiDocumentDescriptor document)
        {
            var mapped = _documentMapper.Map(document);

            var serializerVersion = mapped.Asyncapi switch
            {
                string version when version.StartsWith("2.") => AsyncApiVersion.AsyncApi2_0,
                string version when version.StartsWith("3.") => AsyncApiVersion.AsyncApi3_0,
                _ => throw new InvalidOperationException($"Unsupported AsyncAPI version '{mapped.Asyncapi ?? "<null>"}'.")
            };

            if (serializerVersion == AsyncApiVersion.AsyncApi3_0)
            {
                NormalizeNullabilityForAsyncApi3(mapped);
            }

            return mapped.SerializeAsJson(serializerVersion);
        }

        private static void NormalizeNullabilityForAsyncApi3(AsyncApiDocument document)
        {
            foreach (var schemaId in document.Components.Schemas.Keys.ToArray())
            {
                var multiFormatSchema = document.Components.Schemas[schemaId];
                if (multiFormatSchema.Schema is AsyncApiJsonSchema schema)
                {
                    multiFormatSchema.Schema = RewriteNullableSchema(schema);
                }
            }
        }

        private static AsyncApiJsonSchema RewriteNullableSchema(AsyncApiJsonSchema schema)
        {
            var normalized = CloneSchemaWithoutNullableKeyword(schema);
            if (!IsNullableSchema(schema))
            {
                return normalized;
            }

            var wrapper = new AsyncApiJsonSchema
            {
                Title = IsReferenceSchema(normalized) ? null : normalized.Title,
            };

            if (!IsReferenceSchema(normalized))
            {
                normalized.Title = null;
            }

            wrapper.OneOf.Add(normalized);
            wrapper.OneOf.Add(new AsyncApiJsonSchema
            {
                Type = SchemaType.Null,
            });

            return wrapper;
        }

        private static AsyncApiJsonSchema CloneSchemaWithoutNullableKeyword(AsyncApiJsonSchema schema)
        {
            if (TryGetReference(schema, out var reference))
            {
                return new AsyncApiJsonSchemaReference(reference);
            }

            var clone = new AsyncApiJsonSchema
            {
                Title = schema.Title,
                Type = schema.Type,
                Format = schema.Format,
            };

            if (schema.Items is not null)
            {
                clone.Items = RewriteNullableSchema(schema.Items);
            }

            foreach (var pair in schema.Properties)
            {
                clone.Properties[pair.Key] = RewriteNullableSchema(pair.Value);
            }

            foreach (var required in schema.Required)
            {
                clone.Required.Add(required);
            }

            foreach (var value in schema.Enum)
            {
                clone.Enum.Add(value);
            }

            foreach (var item in schema.OneOf)
            {
                clone.OneOf.Add(RewriteNullableSchema(item));
            }

            foreach (var item in schema.AnyOf)
            {
                clone.AnyOf.Add(RewriteNullableSchema(item));
            }

            foreach (var item in schema.AllOf)
            {
                clone.AllOf.Add(RewriteNullableSchema(item));
            }

            if (schema.AdditionalProperties is not null)
            {
                clone.AdditionalProperties = RewriteNullableSchema(schema.AdditionalProperties);
            }

            return clone;
        }

        private static bool IsReferenceSchema(AsyncApiJsonSchema schema)
        {
            return TryGetReference(schema, out _);
        }

        private static bool IsNullableSchema(AsyncApiJsonSchema schema)
        {
            return !IsReferenceSchema(schema) && schema.Nullable;
        }

        private static bool TryGetReference(AsyncApiJsonSchema schema, out string reference)
        {
            if (schema is AsyncApiJsonSchemaReference schemaReference)
            {
                reference = schemaReference.Reference.Reference;
                return !string.IsNullOrWhiteSpace(reference);
            }

            reference = string.Empty;
            return false;
        }
    }
}
