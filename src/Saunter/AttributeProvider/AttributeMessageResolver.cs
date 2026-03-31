using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Saunter.AttributeProvider.Attributes;
using Saunter.AttributeProvider.Descriptors;
using Saunter.SharedKernel.Descriptors;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.AttributeProvider
{
    internal class AttributeMessageResolver : IAttributeMessageResolver
    {
        private readonly IAsyncApiSchemaGenerator _schemaGenerator;

        public AttributeMessageResolver(IAsyncApiSchemaGenerator schemaGenerator)
        {
            _schemaGenerator = schemaGenerator;
        }

        public AsyncApiMessageResolutionDescriptor ResolveForOperation(MethodInfo method, OperationAttribute operationAttribute)
        {
            var messageAttributes = method.GetCustomAttributes<MessageAttribute>().ToArray();
            if (messageAttributes.Any())
            {
                return GenerateMessagesFromAttributes(messageAttributes);
            }

            if (operationAttribute.MessagePayloadType is not null)
            {
                return GenerateMessageFromType(operationAttribute.MessagePayloadType.GetTypeInfo());
            }

            return new AsyncApiMessageResolutionDescriptor(Array.Empty<string>(), Array.Empty<AsyncApiMessageDescriptor>(), Array.Empty<AsyncApiSchemaComponentDescriptor>());
        }

        public AsyncApiMessageResolutionDescriptor ResolveForOperation(TypeInfo type, OperationAttribute operationAttribute)
        {
            var messageAttributes = type
                .DeclaredMethods
                .SelectMany(method => method.GetCustomAttributes<MessageAttribute>())
                .ToArray();

            if (messageAttributes.Any())
            {
                return GenerateMessagesFromAttributes(messageAttributes);
            }

            if (operationAttribute.MessagePayloadType is not null)
            {
                return GenerateMessageFromType(operationAttribute.MessagePayloadType.GetTypeInfo());
            }

            return new AsyncApiMessageResolutionDescriptor(Array.Empty<string>(), Array.Empty<AsyncApiMessageDescriptor>(), Array.Empty<AsyncApiSchemaComponentDescriptor>());
        }

        private AsyncApiMessageResolutionDescriptor GenerateMessagesFromAttributes(IEnumerable<MessageAttribute> messageAttributes)
        {
            var messageDescriptors = new List<AsyncApiMessageDescriptor>();
            var schemaDescriptors = new List<AsyncApiSchemaComponentDescriptor>();

            foreach (var attribute in messageAttributes)
            {
                var result = GenerateMessageFromAttribute(attribute);
                if (result is not MessageDescriptorResult resolved)
                {
                    continue;
                }

                messageDescriptors.Add(resolved.Message);
                schemaDescriptors.AddRange(resolved.Schemas);
            }

            return new AsyncApiMessageResolutionDescriptor(
                messageDescriptors.Select(message => message.Id).Distinct(StringComparer.Ordinal).ToArray(),
                messageDescriptors
                    .DistinctBy(message => message.Id)
                    .ToArray(),
                schemaDescriptors
                    .DistinctBy(schema => schema.Id)
                    .ToArray());
        }

        private AsyncApiMessageResolutionDescriptor GenerateMessageFromType(TypeInfo payloadType)
        {
            var payloadSchema = GetAsyncApiSchemaReference(payloadType);
            var messageId = payloadSchema?.Id;

            if (string.IsNullOrWhiteSpace(messageId))
            {
                return new AsyncApiMessageResolutionDescriptor(Array.Empty<string>(), Array.Empty<AsyncApiMessageDescriptor>(), payloadSchema?.Schemas ?? Array.Empty<AsyncApiSchemaComponentDescriptor>());
            }

            var message = new AsyncApiMessageDescriptor(
                messageId,
                messageId,
                messageId,
                null,
                null,
                payloadSchema?.Id,
                null,
                null,
                null,
                null,
                null,
                null,
                Array.Empty<string>());

            return new AsyncApiMessageResolutionDescriptor(
                [messageId],
                [message],
                payloadSchema?.Schemas ?? Array.Empty<AsyncApiSchemaComponentDescriptor>());
        }

        private MessageDescriptorResult? GenerateMessageFromAttribute(MessageAttribute messageAttribute)
        {
            if (messageAttribute.PayloadType == null)
            {
                return null;
            }

            var payloadSchema = GetAsyncApiSchemaReference(messageAttribute.PayloadType.GetTypeInfo());
            var messageId = AttributeProviderModelFactory.SanitizeComponentKey(messageAttribute.MessageId ?? payloadSchema?.Id ?? messageAttribute.PayloadType.Name);

            var headersSchema = GetHeadersSchemaReference(messageAttribute.HeadersType?.GetTypeInfo());
            var message = new AsyncApiMessageDescriptor(
                messageId,
                messageAttribute.Name ?? payloadSchema?.Id ?? messageAttribute.PayloadType.Name,
                messageAttribute.Title ?? payloadSchema?.Id ?? messageAttribute.PayloadType.Name,
                messageAttribute.Summary,
                messageAttribute.Description,
                payloadSchema?.Id,
                headersSchema?.Id,
                messageAttribute.CorrelationId,
                messageAttribute.ContentType,
                messageAttribute.ExternalDocs,
                messageAttribute.ExternalDocsDescription,
                messageAttribute.BindingsRef,
                messageAttribute.Tags ?? Array.Empty<string>());

            return new MessageDescriptorResult(
                message,
                (payloadSchema?.Schemas ?? Array.Empty<AsyncApiSchemaComponentDescriptor>())
                    .Concat(headersSchema?.Schemas ?? Array.Empty<AsyncApiSchemaComponentDescriptor>())
                    .DistinctBy(schema => schema.Id)
                    .ToArray());
        }

        private SchemaReferenceInfo? GetAsyncApiSchemaReference(TypeInfo? payloadType)
        {
            var generatedSchemas = _schemaGenerator.Generate(payloadType?.AsType());
            if (generatedSchemas is null)
            {
                return null;
            }

            var schemas = generatedSchemas.Value.All
                .Where(schema => !string.IsNullOrWhiteSpace(schema.Id))
                .Select(schema => new AsyncApiSchemaComponentDescriptor(schema.Id!, schema))
                .ToArray();

            return new SchemaReferenceInfo(
                generatedSchemas.Value.Root.Id!,
                schemas);
        }

        private SchemaReferenceInfo? GetHeadersSchemaReference(TypeInfo? headersType)
        {
            if (headersType is null)
            {
                return null;
            }

            var generatedSchema = _schemaGenerator.Generate(headersType.AsType());
            if (generatedSchema is null || !IsObjectLikeSchema(generatedSchema.Value.Root))
            {
                throw new InvalidOperationException($"Headers type '{headersType.Name}' must generate an object schema.");
            }

            return GetAsyncApiSchemaReference(headersType);
        }

        private static bool IsObjectLikeSchema(AsyncApiSchemaDescriptor schema)
        {
            if (schema.Type == AsyncApiSchemaValueType.Object)
            {
                return true;
            }

            if (schema.OneOf.Count == 0)
            {
                return false;
            }

            return schema.OneOf.Any(item => item.Type == AsyncApiSchemaValueType.Object);
        }

        private readonly record struct SchemaReferenceInfo(string Id, IReadOnlyList<AsyncApiSchemaComponentDescriptor> Schemas);
        private readonly record struct MessageDescriptorResult(AsyncApiMessageDescriptor Message, IReadOnlyList<AsyncApiSchemaComponentDescriptor> Schemas);
    }
}
