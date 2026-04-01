using System.Linq;
using ByteBard.AsyncAPI.Models;
using Saunter.SharedKernel.Descriptors;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.SharedKernel
{
    internal class AsyncApiSchemaMapper : IAsyncApiSchemaMapper
    {
        public AsyncApiJsonSchema Map(AsyncApiSchemaDescriptor descriptor)
        {
            if (!string.IsNullOrWhiteSpace(descriptor.Reference))
            {
                return new AsyncApiJsonSchemaReference(descriptor.Reference);
            }

            var schema = new AsyncApiJsonSchema
            {
                Title = descriptor.Id,
                Type = MapSchemaType(descriptor.Type),
                Format = descriptor.Format,
                Nullable = descriptor.Nullable,
            };

            foreach (var pair in descriptor.Properties)
            {
                schema.Properties[pair.Key] = Map(pair.Value);
            }

            foreach (var propertyName in descriptor.Required)
            {
                schema.Required.Add(propertyName);
            }

            foreach (var value in descriptor.EnumValues)
            {
                schema.Enum.Add(new AsyncApiAny(value));
            }

            if (descriptor.Items is not null)
            {
                schema.Items = Map(descriptor.Items);
            }

            foreach (var item in descriptor.OneOf)
            {
                schema.OneOf.Add(Map(item));
            }

            foreach (var item in descriptor.AllOf)
            {
                schema.AllOf.Add(Map(item));
            }

            return schema;
        }

        private static SchemaType? MapSchemaType(AsyncApiSchemaValueType? valueType)
        {
            return valueType switch
            {
                AsyncApiSchemaValueType.Boolean => SchemaType.Boolean,
                AsyncApiSchemaValueType.Integer => SchemaType.Integer,
                AsyncApiSchemaValueType.Number => SchemaType.Number,
                AsyncApiSchemaValueType.String => SchemaType.String,
                AsyncApiSchemaValueType.Object => SchemaType.Object,
                AsyncApiSchemaValueType.Array => SchemaType.Array,
                _ => null,
            };
        }
    }
}
