using System;
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
            foreach (var schemaId in global::System.Linq.Enumerable.ToArray(document.Components.Schemas.Keys))
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
            if (!normalized.Nullable)
            {
                return normalized;
            }

            normalized.Nullable = false;
            var wrapper = new AsyncApiJsonSchema
            {
                Title = normalized.Title,
            };
            normalized.Title = null;
            wrapper.OneOf.Add(normalized);
            wrapper.OneOf.Add(new AsyncApiJsonSchema
            {
                Type = SchemaType.Null,
            });

            return wrapper;
        }

        private static AsyncApiJsonSchema CloneSchemaWithoutNullableKeyword(AsyncApiJsonSchema schema)
        {
            var clone = schema is AsyncApiJsonSchemaReference schemaReference
                ? new AsyncApiJsonSchemaReference(schemaReference.Reference.Reference)
                : new AsyncApiJsonSchema();

            clone.Title = schema.Title;
            clone.Type = schema.Type;
            clone.Format = schema.Format;
            clone.Nullable = schema.Nullable;

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
    }
}
