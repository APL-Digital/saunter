using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Saunter.AttributeProvider.Attributes;
using Saunter.AttributeProvider.Descriptors;
using Saunter.Options;
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

        public AsyncApiMessageResolutionDescriptor ResolveForOperation(MethodInfo method, OperationAttribute operationAttribute, AsyncApiInferenceOptions inferenceOptions)
        {
            var messageAttributes = method.GetCustomAttributes<MessageAttribute>().ToArray();
            if (messageAttributes.Any())
            {
                var resolution = GenerateMessagesFromAttributes(messageAttributes, inferenceOptions);
                if (resolution.Messages.Any())
                {
                    return resolution;
                }
            }

            if (operationAttribute.MessagePayloadType is not null)
            {
                return GenerateMessageFromType(operationAttribute.MessagePayloadType.GetTypeInfo(), inferenceOptions);
            }

            if (inferenceOptions.InferPayloadTypeFromMethodSignature && TryInferPayloadTypes(method).Any())
            {
                return GenerateMessagesFromTypes(TryInferPayloadTypes(method), inferenceOptions);
            }

            return new AsyncApiMessageResolutionDescriptor(Array.Empty<string>(), Array.Empty<AsyncApiMessageDescriptor>(), Array.Empty<AsyncApiSchemaComponentDescriptor>());
        }

        public AsyncApiMessageResolutionDescriptor ResolveForOperation(TypeInfo type, OperationAttribute operationAttribute, AsyncApiInferenceOptions inferenceOptions)
        {
            var candidateMethods = GetCandidateOperationMethods(type).ToArray();
            var messageAttributes = candidateMethods
                .SelectMany(method => method.GetCustomAttributes<MessageAttribute>())
                .ToArray();

            if (messageAttributes.Any())
            {
                var resolution = GenerateMessagesFromAttributes(messageAttributes, inferenceOptions);
                if (resolution.Messages.Any())
                {
                    return resolution;
                }
            }

            if (operationAttribute.MessagePayloadType is not null)
            {
                return GenerateMessageFromType(operationAttribute.MessagePayloadType.GetTypeInfo(), inferenceOptions);
            }

            if (inferenceOptions.InferPayloadTypeFromMethodSignature)
            {
                var inferredTypes = candidateMethods
                    .SelectMany(TryInferPayloadTypes)
                    .Distinct()
                    .ToArray();
                if (inferredTypes.Any())
                {
                    return GenerateMessagesFromTypes(inferredTypes, inferenceOptions);
                }
            }

            return new AsyncApiMessageResolutionDescriptor(Array.Empty<string>(), Array.Empty<AsyncApiMessageDescriptor>(), Array.Empty<AsyncApiSchemaComponentDescriptor>());
        }

        private AsyncApiMessageResolutionDescriptor GenerateMessagesFromAttributes(IEnumerable<MessageAttribute> messageAttributes, AsyncApiInferenceOptions inferenceOptions)
        {
            var messageDescriptors = new List<AsyncApiMessageDescriptor>();
            var schemaDescriptors = new List<AsyncApiSchemaComponentDescriptor>();

            foreach (var attribute in messageAttributes)
            {
                var result = GenerateMessageFromAttribute(attribute, inferenceOptions);
                if (result is not MessageDescriptorResult resolved)
                {
                    continue;
                }

                messageDescriptors.Add(resolved.Message);
                schemaDescriptors.AddRange(resolved.Schemas);
            }

            return CreateMessageResolution(messageDescriptors, schemaDescriptors);
        }

        private AsyncApiMessageResolutionDescriptor GenerateMessagesFromTypes(IEnumerable<Type> payloadTypes, AsyncApiInferenceOptions inferenceOptions)
        {
            var resolutions = payloadTypes
                .Select(type => GenerateMessageFromType(type.GetTypeInfo(), inferenceOptions))
                .ToArray();

            return CreateMessageResolution(
                resolutions.SelectMany(resolution => resolution.Messages),
                resolutions.SelectMany(resolution => resolution.Schemas));
        }

        private AsyncApiMessageResolutionDescriptor GenerateMessageFromType(TypeInfo payloadType, AsyncApiInferenceOptions inferenceOptions)
        {
            var payloadSchema = GetAsyncApiSchemaReference(payloadType);
            var messageId = payloadSchema?.Id ?? AttributeProviderModelFactory.SanitizeComponentKey(inferenceOptions.MessageNameGenerator(payloadType.AsType()));

            if (string.IsNullOrWhiteSpace(messageId))
            {
                return new AsyncApiMessageResolutionDescriptor(Array.Empty<string>(), Array.Empty<AsyncApiMessageDescriptor>(), payloadSchema?.Schemas ?? Array.Empty<AsyncApiSchemaComponentDescriptor>());
            }

            var message = new AsyncApiMessageDescriptor(
                messageId,
                inferenceOptions.MessageNameGenerator(payloadType.AsType()),
                inferenceOptions.MessageTitleGenerator(payloadType.AsType()),
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

        private MessageDescriptorResult? GenerateMessageFromAttribute(MessageAttribute messageAttribute, AsyncApiInferenceOptions inferenceOptions)
        {
            if (messageAttribute.PayloadType == null)
            {
                return null;
            }

            var payloadSchema = GetAsyncApiSchemaReference(messageAttribute.PayloadType.GetTypeInfo());
            var messageId = messageAttribute.MessageId is string explicitMessageId
                ? AttributeProviderModelFactory.SanitizeComponentKey(explicitMessageId)
                : payloadSchema?.Id ?? AttributeProviderModelFactory.SanitizeComponentKey(messageAttribute.PayloadType.Name);
            if (string.IsNullOrWhiteSpace(messageId))
            {
                return null;
            }

            var headersSchema = GetHeadersSchemaReference(messageAttribute.HeadersType?.GetTypeInfo());
            var message = new AsyncApiMessageDescriptor(
                messageId,
                messageAttribute.Name ?? inferenceOptions.MessageNameGenerator(messageAttribute.PayloadType),
                messageAttribute.Title ?? inferenceOptions.MessageTitleGenerator(messageAttribute.PayloadType),
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
                DeduplicateSchemaDescriptors(
                    (payloadSchema?.Schemas ?? Array.Empty<AsyncApiSchemaComponentDescriptor>())
                        .Concat(headersSchema?.Schemas ?? Array.Empty<AsyncApiSchemaComponentDescriptor>())));
        }

        private static AsyncApiMessageResolutionDescriptor CreateMessageResolution(
            IEnumerable<AsyncApiMessageDescriptor> messageDescriptors,
            IEnumerable<AsyncApiSchemaComponentDescriptor> schemaDescriptors)
        {
            var deduplicatedMessages = DeduplicateMessageDescriptors(messageDescriptors);
            return new AsyncApiMessageResolutionDescriptor(
                deduplicatedMessages.Select(message => message.Id).ToArray(),
                deduplicatedMessages,
                DeduplicateSchemaDescriptors(schemaDescriptors));
        }

        private static AsyncApiMessageDescriptor[] DeduplicateMessageDescriptors(IEnumerable<AsyncApiMessageDescriptor> messageDescriptors)
        {
            return DeduplicateById(
                messageDescriptors,
                message => message.Id,
                MessageDescriptorsMatch,
                "message",
                FormatMessageDescriptor);
        }

        private static AsyncApiSchemaComponentDescriptor[] DeduplicateSchemaDescriptors(IEnumerable<AsyncApiSchemaComponentDescriptor> schemaDescriptors)
        {
            return DeduplicateById(
                schemaDescriptors,
                schema => schema.Id,
                SchemaDescriptorsMatch,
                "schema",
                FormatSchemaComponentDescriptor);
        }

        private static TDescriptor[] DeduplicateById<TDescriptor>(
            IEnumerable<TDescriptor> descriptors,
            Func<TDescriptor, string> idSelector,
            Func<TDescriptor, TDescriptor, bool> descriptorsMatch,
            string descriptorType,
            Func<TDescriptor, string> formatDescriptor)
        {
            var deduplicatedDescriptors = new List<TDescriptor>();

            foreach (var descriptorsById in descriptors.GroupBy(idSelector, StringComparer.Ordinal))
            {
                var representative = descriptorsById.First();
                foreach (var candidate in descriptorsById.Skip(1))
                {
                    if (!descriptorsMatch(representative, candidate))
                    {
                        throw new InvalidOperationException(
                            $"Conflicting {descriptorType} descriptors found for id '{descriptorsById.Key}'. " +
                            $"Existing definition: {formatDescriptor(representative)}. Incoming definition: {formatDescriptor(candidate)}.");
                    }
                }

                deduplicatedDescriptors.Add(representative);
            }

            return deduplicatedDescriptors.ToArray();
        }

        private static bool MessageDescriptorsMatch(AsyncApiMessageDescriptor source, AsyncApiMessageDescriptor additional)
        {
            return string.Equals(source.Id, additional.Id, StringComparison.Ordinal)
                && string.Equals(source.Name, additional.Name, StringComparison.Ordinal)
                && string.Equals(source.Title, additional.Title, StringComparison.Ordinal)
                && string.Equals(source.Summary, additional.Summary, StringComparison.Ordinal)
                && string.Equals(source.Description, additional.Description, StringComparison.Ordinal)
                && string.Equals(source.PayloadSchemaId, additional.PayloadSchemaId, StringComparison.Ordinal)
                && string.Equals(source.HeadersSchemaId, additional.HeadersSchemaId, StringComparison.Ordinal)
                && string.Equals(source.CorrelationIdRef, additional.CorrelationIdRef, StringComparison.Ordinal)
                && string.Equals(source.ContentType, additional.ContentType, StringComparison.Ordinal)
                && string.Equals(source.ExternalDocsUrl, additional.ExternalDocsUrl, StringComparison.Ordinal)
                && string.Equals(source.ExternalDocsDescription, additional.ExternalDocsDescription, StringComparison.Ordinal)
                && string.Equals(source.BindingsRef, additional.BindingsRef, StringComparison.Ordinal)
                && source.Tags.SequenceEqual(additional.Tags, StringComparer.Ordinal);
        }

        private static bool SchemaDescriptorsMatch(AsyncApiSchemaComponentDescriptor source, AsyncApiSchemaComponentDescriptor additional)
        {
            return string.Equals(source.Id, additional.Id, StringComparison.Ordinal)
                && SchemaDescriptorsMatch(source.Schema, additional.Schema);
        }

        private static bool SchemaDescriptorsMatch(AsyncApiSchemaDescriptor source, AsyncApiSchemaDescriptor additional)
        {
            if (!string.Equals(source.Id, additional.Id, StringComparison.Ordinal)
                || source.Type != additional.Type
                || !string.Equals(source.Format, additional.Format, StringComparison.Ordinal)
                || source.Nullable != additional.Nullable
                || !string.Equals(source.Reference, additional.Reference, StringComparison.Ordinal))
            {
                return false;
            }

            if (!NullableSchemaDescriptorsMatch(source.Items, additional.Items))
            {
                return false;
            }

            if (!NullableSchemaDescriptorsMatch(source.AdditionalProperties, additional.AdditionalProperties))
            {
                return false;
            }

            if (!source.Required.SequenceEqual(additional.Required, StringComparer.Ordinal)
                || !source.EnumValues.SequenceEqual(additional.EnumValues, StringComparer.Ordinal)
                || !source.OneOf.Zip(additional.OneOf, SchemaDescriptorsMatch).All(result => result)
                || source.OneOf.Count != additional.OneOf.Count
                || !source.AllOf.Zip(additional.AllOf, SchemaDescriptorsMatch).All(result => result)
                || source.AllOf.Count != additional.AllOf.Count)
            {
                return false;
            }

            if (source.Properties.Count != additional.Properties.Count)
            {
                return false;
            }

            foreach (var property in source.Properties)
            {
                if (!additional.Properties.TryGetValue(property.Key, out var additionalProperty)
                    || !SchemaDescriptorsMatch(property.Value, additionalProperty))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool NullableSchemaDescriptorsMatch(AsyncApiSchemaDescriptor? source, AsyncApiSchemaDescriptor? additional)
        {
            if (source is null || additional is null)
            {
                return source is null && additional is null;
            }

            return SchemaDescriptorsMatch(source, additional);
        }

        private static AsyncApiSchemaDescriptor CreateNullableRootWrapperComponent(
            TypeInfo payloadType,
            AsyncApiSchemaDescriptor root,
            IReadOnlyCollection<AsyncApiSchemaComponentDescriptor> existingSchemas)
        {
            var baseSchemaId = root.AllOf
                .Select(item => item.Reference)
                .Where(reference => !string.IsNullOrWhiteSpace(reference) && reference!.StartsWith("#/components/schemas/", StringComparison.Ordinal))
                .Select(reference => reference!["#/components/schemas/".Length..])
                .FirstOrDefault()
                ?? existingSchemas.Select(schema => schema.Id).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(baseSchemaId))
            {
                throw new InvalidOperationException(
                    $"Generated schema root for payload type '{payloadType.AsType()}' is missing a reusable component id.");
            }

            var wrapper = CloneSchema(root);
            wrapper.Id = baseSchemaId + "Nullable";
            return wrapper;
        }

        private static AsyncApiSchemaDescriptor CloneSchema(AsyncApiSchemaDescriptor schema)
        {
            var clone = new AsyncApiSchemaDescriptor
            {
                Id = schema.Id,
                Type = schema.Type,
                Format = schema.Format,
                Nullable = schema.Nullable,
                Reference = schema.Reference,
                Items = schema.Items is null ? null : CloneSchema(schema.Items),
                AdditionalProperties = schema.AdditionalProperties is null ? null : CloneSchema(schema.AdditionalProperties),
            };

            foreach (var pair in schema.Properties)
            {
                clone.Properties[pair.Key] = CloneSchema(pair.Value);
            }

            foreach (var required in schema.Required)
            {
                clone.Required.Add(required);
            }

            foreach (var value in schema.EnumValues)
            {
                clone.EnumValues.Add(value);
            }

            foreach (var item in schema.OneOf)
            {
                clone.OneOf.Add(CloneSchema(item));
            }

            foreach (var item in schema.AllOf)
            {
                clone.AllOf.Add(CloneSchema(item));
            }

            return clone;
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
                .ToList();

            if (generatedSchemas.Value.Root.Id is string rootId)
            {
                return new SchemaReferenceInfo(
                    rootId,
                    schemas);
            }

            if (payloadType is null)
            {
                throw new InvalidOperationException("Generated schema root is missing an id and no payload type was provided to identify the failing schema generation path.");
            }

            var wrapperComponent = CreateNullableRootWrapperComponent(payloadType, generatedSchemas.Value.Root, schemas);
            schemas.Add(new AsyncApiSchemaComponentDescriptor(wrapperComponent.Id!, wrapperComponent));

            return new SchemaReferenceInfo(
                wrapperComponent.Id!,
                DeduplicateSchemaDescriptors(schemas));
        }

        private SchemaReferenceInfo? GetHeadersSchemaReference(TypeInfo? headersType)
        {
            if (headersType is null)
            {
                return null;
            }

            var generatedSchema = _schemaGenerator.Generate(headersType.AsType());
            var generatedRoot = generatedSchema?.Root;
            if (generatedSchema is null || !IsObjectLikeSchema(generatedSchema.Value.Root))
            {
                throw new InvalidOperationException(
                    $"Headers type '{headersType.AsType().FullName}' must generate an object-like schema, but generated {FormatSchemaDescriptor(generatedRoot)}.");
            }

            return GetAsyncApiSchemaReference(headersType);
        }

        private static bool IsObjectLikeSchema(AsyncApiSchemaDescriptor schema)
        {
            if (schema.Type == AsyncApiSchemaValueType.Object)
            {
                return true;
            }

            if (schema.OneOf.Any(IsObjectLikeSchema))
            {
                return true;
            }

            return schema.AllOf.Any(IsObjectLikeSchema);
        }

        private static IEnumerable<MethodInfo> GetCandidateOperationMethods(TypeInfo type)
        {
            return type.DeclaredMethods.Where(method =>
                !method.IsSpecialName
                && !method.IsConstructor
                && !method.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false));
        }

        private static IEnumerable<Type> TryInferPayloadTypes(MethodInfo method)
        {
            var consumeContextParameter = method.GetParameters()
                .FirstOrDefault(parameter =>
                    parameter.ParameterType.IsGenericType
                    && parameter.ParameterType.GetGenericTypeDefinition().FullName == "MassTransit.ConsumeContext`1");
            if (consumeContextParameter is not null)
            {
                yield return consumeContextParameter.ParameterType.GenericTypeArguments[0];
                yield break;
            }

            var parameters = method.GetParameters()
                .Where(parameter => !IsIgnorableParameter(parameter.ParameterType))
                .ToArray();
            if (parameters.Length == 1)
            {
                yield return parameters[0].ParameterType;
            }
        }

        private static bool IsIgnorableParameter(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset))
            {
                return true;
            }

            if (type == typeof(System.Threading.CancellationToken))
            {
                return true;
            }

            if (typeInfo.IsPrimitive || typeInfo.IsEnum)
            {
                return true;
            }

            return Nullable.GetUnderlyingType(type) is { } underlying && IsIgnorableParameter(underlying);
        }

        private static string FormatMessageDescriptor(AsyncApiMessageDescriptor descriptor)
        {
            return $"id={FormatValue(descriptor.Id)}, name={FormatValue(descriptor.Name)}, title={FormatValue(descriptor.Title)}, payload={FormatValue(descriptor.PayloadSchemaId)}, headers={FormatValue(descriptor.HeadersSchemaId)}, contentType={FormatValue(descriptor.ContentType)}, bindings={FormatValue(descriptor.BindingsRef)}, tags={FormatValues(descriptor.Tags)}";
        }

        private static string FormatSchemaComponentDescriptor(AsyncApiSchemaComponentDescriptor descriptor)
        {
            return $"id={FormatValue(descriptor.Id)}, schema={FormatSchemaDescriptor(descriptor.Schema)}";
        }

        private static string FormatSchemaDescriptor(AsyncApiSchemaDescriptor? descriptor)
        {
            if (descriptor is null)
            {
                return "<null>";
            }

            return $"id={FormatValue(descriptor.Id)}, type={descriptor.Type?.ToString() ?? "<null>"}, format={FormatValue(descriptor.Format)}, nullable={descriptor.Nullable}, reference={FormatValue(descriptor.Reference)}, properties={FormatValues(descriptor.Properties.Keys)}, oneOfCount={descriptor.OneOf.Count}, allOfCount={descriptor.AllOf.Count}";
        }

        private static string FormatValues(IEnumerable<string> values)
        {
            var materialized = values.ToArray();
            return materialized.Length == 0
                ? "[]"
                : $"[{string.Join(", ", materialized.Select(FormatValue))}]";
        }

        private static string FormatValue(string? value)
        {
            return value is null ? "<null>" : $"'{value}'";
        }

        private readonly record struct SchemaReferenceInfo(string Id, IReadOnlyList<AsyncApiSchemaComponentDescriptor> Schemas);
        private readonly record struct MessageDescriptorResult(AsyncApiMessageDescriptor Message, IReadOnlyList<AsyncApiSchemaComponentDescriptor> Schemas);
    }
}
