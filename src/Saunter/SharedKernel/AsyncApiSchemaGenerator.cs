using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Saunter.Options;
using Saunter.SharedKernel.Descriptors;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.SharedKernel
{
    /// <summary>
    /// Default <see cref="IAsyncApiSchemaGenerator"/> implementation that reflects over CLR types
    /// to produce JSON Schema descriptors, honoring System.Text.Json attributes and nullability annotations.
    /// </summary>
    public class AsyncApiSchemaGenerator : IAsyncApiSchemaGenerator
    {
        private readonly Func<PropertyInfo, string> _propertyNameSelector;

        /// <summary>
        /// Creates a generator with default <see cref="AsyncApiOptions"/> (camelCase property names).
        /// </summary>
        public AsyncApiSchemaGenerator()
            : this(Microsoft.Extensions.Options.Options.Create(new AsyncApiOptions()))
        {
        }

        /// <summary>
        /// Creates a generator using <see cref="AsyncApiOptions.PropertyNameSelector"/> from <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The options controlling schema property naming.</param>
        public AsyncApiSchemaGenerator(IOptions<AsyncApiOptions> options)
        {
            _propertyNameSelector = options.Value.PropertyNameSelector ?? DefaultPropertyNameSelector;
        }

        /// <inheritdoc />
        public GeneratedSchemaDescriptors? Generate(Type? type)
        {
            var nullabilityInfoContext = new NullabilityInfoContext();
            var generationContext = new SchemaGenerationContext();
            var generatedSchemas = GenerateBranch(type, new HashSet<string>(StringComparer.Ordinal), nullabilityInfoContext, generationContext, isRoot: true);
            if (generatedSchemas is null)
            {
                return null;
            }

            var allSchemas = new List<AsyncApiSchemaDescriptor>();
            if (!string.IsNullOrWhiteSpace(generatedSchemas.Value.Root.Id))
            {
                allSchemas.Add(generatedSchemas.Value.Root);
            }

            allSchemas.AddRange(generatedSchemas.Value.All);

            return new(
                generatedSchemas.Value.Root,
                DeduplicateSchemas(
                    allSchemas.Where(schema => !string.IsNullOrWhiteSpace(schema.Id)),
                    "building the generated schema set"));
        }

        private GeneratedSchemaDescriptors? GenerateBranch(
            Type? type,
            HashSet<string> parents,
            NullabilityInfoContext nullabilityInfoContext,
            SchemaGenerationContext generationContext,
            NullabilityInfo? nullabilityInfo = null,
            bool isRoot = false)
        {
            if (type is null)
            {
                return null;
            }

            var typeInfo = type.GetTypeInfo();
            var isNullable = IsNullable(typeInfo, nullabilityInfo, isRoot);

            if (Nullable.GetUnderlyingType(type) is Type underlyingType)
            {
                type = underlyingType;
                typeInfo = type.GetTypeInfo();
                isNullable = true;
            }

            // JsonElement represents the JSON value itself, not its CLR reflection surface,
            // and System.Text.Json writes an object-typed member as whatever JSON its runtime
            // value serializes to (and reads it back as a JsonElement). An empty JSON Schema
            // correctly permits any JSON value, including null, for both.
            if (type == typeof(JsonElement) || type == typeof(object))
            {
                return new(new AsyncApiSchemaDescriptor(), Array.Empty<AsyncApiSchemaDescriptor>());
            }

            var schemaType = MapJsonTypeToSchemaType(typeInfo);
            var isDictionary = TryGetDictionaryValueType(typeInfo, out var dictionaryValueType);
            var collectionNullability = GetCollectionNullabilityDiscriminator(isDictionary, schemaType, nullabilityInfo);
            var name = GetSchemaId(typeInfo, schemaType, generationContext, isRoot, collectionNullability);
            var schema = new AsyncApiSchemaDescriptor
            {
                Id = name,
                Type = schemaType,
                Description = type.GetCustomAttribute<DescriptionAttribute>()?.Description,
            };

            if (schema.Type is not AsyncApiSchemaValueType.Object and not AsyncApiSchemaValueType.Array)
            {
                if (typeInfo.IsEnum)
                {
                    schema.Format = "enum";
                    foreach (var value in GetEnumValues(typeInfo))
                    {
                        schema.EnumValues.Add(value);
                    }
                }
                else if (typeInfo.AsType() == typeof(byte[]))
                {
                    schema.Format = "byte";
                }
                else
                {
                    schema.Format = name;
                }

                var usageSchema = CreateUsageSchema(schema, isNullable);
                var sharedSchemas = ReferenceEquals(usageSchema, schema)
                    ? Array.Empty<AsyncApiSchemaDescriptor>()
                    : [schema];
                return new(usageSchema, sharedSchemas);
            }

            if (schema.Type == AsyncApiSchemaValueType.Array)
            {
                var itemSchemas = new List<AsyncApiSchemaDescriptor>();
                var itemType = GetEnumerableItemType(typeInfo);
                var generatedItemSchema = GenerateBranch(itemType, parents, nullabilityInfoContext, generationContext, GetItemNullabilityInfo(nullabilityInfo));
                if (generatedItemSchema is not null)
                {
                    schema.Items = generatedItemSchema.Value.Root;
                    itemSchemas.AddRange(generatedItemSchema.Value.All);
                }

                var usageSchema = CreateCollectionUsageSchema(schema, isNullable, isRoot);
                if (isRoot && !ReferenceEquals(usageSchema, schema))
                {
                    itemSchemas.Insert(0, schema);
                }

                return new(usageSchema, DeduplicateSchemas(itemSchemas, $"building array items for schema '{name}'"));
            }

            if (isDictionary)
            {
                if (!isRoot && generationContext.ReusableCollectionSchemaIds.Contains(name))
                {
                    var reusableReference = new AsyncApiSchemaDescriptor
                    {
                        Reference = $"#/components/schemas/{name}",
                    };
                    return new(CreateUsageSchema(reusableReference, isNullable), Array.Empty<AsyncApiSchemaDescriptor>());
                }

                if (!parents.Add(name))
                {
                    var referenceSchema = new AsyncApiSchemaDescriptor
                    {
                        Reference = $"#/components/schemas/{name}",
                    };
                    return new(CreateUsageSchema(referenceSchema, isNullable), Array.Empty<AsyncApiSchemaDescriptor>());
                }

                try
                {
                    var dictionarySchemas = new List<AsyncApiSchemaDescriptor>();
                    var generatedValueSchema = GenerateBranch(dictionaryValueType, parents, nullabilityInfoContext, generationContext, GetDictionaryValueNullabilityInfo(nullabilityInfo));
                    if (generatedValueSchema is not null)
                    {
                        schema.AdditionalProperties = generatedValueSchema.Value.Root;
                        dictionarySchemas.AddRange(generatedValueSchema.Value.All);
                    }

                    var usageSchema = CreateCollectionUsageSchema(schema, isNullable, isRoot);
                    var recursiveReference = $"#/components/schemas/{name}";
                    var requiresRecursiveComponent = !isRoot
                        && new[] { schema }
                            .Concat(dictionarySchemas)
                            .Any(candidate => ReferencesComponent(
                                candidate,
                                recursiveReference,
                                new HashSet<AsyncApiSchemaDescriptor>()));
                    if (requiresRecursiveComponent || isRoot && !ReferenceEquals(usageSchema, schema))
                    {
                        dictionarySchemas.Insert(0, schema);
                        generationContext.ReusableCollectionSchemaIds.Add(name);
                    }

                    return new(usageSchema, DeduplicateSchemas(dictionarySchemas, $"building dictionary values for schema '{name}'"));
                }
                finally
                {
                    parents.Remove(name);
                }
            }

            if (!parents.Add(name))
            {
                var referenceSchema = new AsyncApiSchemaDescriptor
                {
                    Reference = $"#/components/schemas/{name}",
                };
                return new(CreateUsageSchema(referenceSchema, isNullable), Array.Empty<AsyncApiSchemaDescriptor>());
            }

            try
            {
                var nestedSchemas = new List<AsyncApiSchemaDescriptor> { schema };
                var derivedTypes = type.GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false).ToArray();
                if (derivedTypes.Length > 0)
                {
                    GeneratePolymorphicAlternatives(type, schema, derivedTypes, nestedSchemas, parents,
                        nullabilityInfoContext, generationContext);
                    return new(CreateUsageSchema(schema, isNullable),
                        DeduplicateSchemas(nestedSchemas, $"building polymorphic alternatives for schema '{name}'"));
                }

                var properties = typeInfo.AsType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetMethod is not null && !p.GetMethod.IsStatic && p.GetIndexParameters().Length == 0)
                    .Where(p => !IsIgnoredForSerialization(p));

                foreach (var prop in properties)
                {
                    var propertyNullability = nullabilityInfoContext.Create(prop);
                    var generatedSchemas = GenerateBranch(prop.PropertyType, parents, nullabilityInfoContext, generationContext, propertyNullability);
                    if (generatedSchemas is null)
                    {
                        continue;
                    }

                    var propertyName = ResolvePropertyName(prop);
                    schema.Properties[propertyName] = ApplyPropertyAnnotations(prop, generatedSchemas.Value.Root);
                    if (IsRequiredProperty(prop, propertyNullability))
                    {
                        schema.Required.Add(propertyName);
                    }

                    nestedSchemas.AddRange(generatedSchemas.Value.All);
                }

                return new(CreateUsageSchema(schema, isNullable), DeduplicateSchemas(nestedSchemas, $"building object properties for schema '{name}'"));
            }
            finally
            {
                parents.Remove(name);
            }
        }

        private void GeneratePolymorphicAlternatives(
            Type baseType,
            AsyncApiSchemaDescriptor schema,
            IReadOnlyList<JsonDerivedTypeAttribute> derivedTypes,
            List<AsyncApiSchemaDescriptor> nestedSchemas,
            HashSet<string> parents,
            NullabilityInfoContext nullabilityInfoContext,
            SchemaGenerationContext generationContext)
        {
            // A concrete base also permits an untagged instance. Our descriptor model
            // cannot express the negative discriminator constraint that would keep that
            // alternative disjoint, so reject it rather than emit an ambiguous oneOf.
            if (!baseType.IsAbstract && !baseType.IsInterface)
            {
                throw new InvalidOperationException(
                    $"Polymorphic type '{baseType}' must be abstract or an interface to export its alternatives.");
            }

            var discriminatorName = baseType.GetCustomAttribute<JsonPolymorphicAttribute>(inherit: false)
                ?.TypeDiscriminatorPropertyName ?? "$type";
            var discriminators = new HashSet<string>(StringComparer.Ordinal);
            foreach (var derived in derivedTypes)
            {
                if (derived.TypeDiscriminator is not string discriminator)
                {
                    throw new InvalidOperationException(
                        $"Polymorphic type '{baseType}' requires a string discriminator for every derived type; " +
                        $"'{derived.DerivedType}' has an unsupported discriminator.");
                }

                if (!discriminators.Add(discriminator)
                    || !baseType.IsAssignableFrom(derived.DerivedType)
                    || derived.DerivedType.IsAbstract || derived.DerivedType.IsInterface)
                {
                    throw new InvalidOperationException(
                        $"Polymorphic type '{baseType}' must declare distinct discriminators and concrete assignable alternatives.");
                }

                var generated = GenerateBranch(derived.DerivedType, parents, nullabilityInfoContext, generationContext)
                    ?? throw new InvalidOperationException($"Cannot generate polymorphic alternative '{derived.DerivedType}'.");
                nestedSchemas.AddRange(generated.All);
                var derivedId = GetSchemaId(derived.DerivedType.GetTypeInfo(), AsyncApiSchemaValueType.Object,
                    generationContext, isRoot: false, collectionNullability: null);
                // Inspect the serialized members directly: a recursive alternative may
                // already be in progress and only return a reference, with no component.
                if (derived.DerivedType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.GetMethod is not null && !property.GetMethod.IsStatic
                        && property.GetIndexParameters().Length == 0 && !IsIgnoredForSerialization(property))
                    .Any(property => ResolvePropertyName(property) == discriminatorName))
                {
                    throw new InvalidOperationException(
                        $"Polymorphic discriminator '{discriminatorName}' conflicts with a serialized property on '{derived.DerivedType}'.");
                }

                // Constrain the tag at the base-type usage site, not on the concrete
                // component: serializing a concrete type directly does not emit a tag.
                var alternative = new AsyncApiSchemaDescriptor { Type = AsyncApiSchemaValueType.Object };
                var tag = new AsyncApiSchemaDescriptor { Type = AsyncApiSchemaValueType.String };
                tag.EnumValues.Add(discriminator);
                var tagConstraint = new AsyncApiSchemaDescriptor { Type = AsyncApiSchemaValueType.Object };
                tagConstraint.Properties.Add(discriminatorName, tag);
                tagConstraint.Required.Add(discriminatorName);
                alternative.AllOf.Add(new AsyncApiSchemaDescriptor { Reference = $"#/components/schemas/{derivedId}" });
                alternative.AllOf.Add(tagConstraint);
                schema.OneOf.Add(alternative);
            }
        }

        private string ResolvePropertyName(PropertyInfo property)
        {
            var propertyName = _propertyNameSelector(property);
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                return propertyName;
            }

            throw new InvalidOperationException(
                $"The configured property name selector returned an empty name for '{property.DeclaringType?.FullName}.{property.Name}'.");
        }

        private static string DefaultPropertyNameSelector(PropertyInfo property)
        {
            return property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? ToSchemaName(property.Name, true);
        }

        private static bool IsIgnoredForSerialization(PropertyInfo property)
        {
            // Only unconditionally-ignored properties are absent from the payload; the
            // conditional variants (WhenWritingNull/WhenWritingDefault) still serialize.
            var ignore = property.GetCustomAttribute<JsonIgnoreAttribute>();
            return ignore is not null && ignore.Condition == JsonIgnoreCondition.Always;
        }

        private static AsyncApiSchemaDescriptor ApplyPropertyAnnotations(PropertyInfo property, AsyncApiSchemaDescriptor schema)
        {
            var description = property.GetCustomAttribute<DescriptionAttribute>();
            var maxLength = property.GetCustomAttribute<MaxLengthAttribute>();
            var minLength = property.GetCustomAttribute<MinLengthAttribute>();
            var stringLength = property.GetCustomAttribute<StringLengthAttribute>();
            var range = property.GetCustomAttribute<RangeAttribute>();
            if (description is null && maxLength is null && minLength is null && stringLength is null && range is null)
            {
                return schema;
            }

            // A property constraint belongs to this usage. It must not change the reusable
            // component or another property with the same CLR type.
            var usage = CloneSchema(schema);
            usage.Id = null;
            if (usage.Reference is not null)
            {
                var reference = usage;
                usage = new AsyncApiSchemaDescriptor();
                usage.AllOf.Add(reference);
            }
            if (description is not null)
            {
                usage.Description = description.Description;
            }

            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var maximum = maxLength is { Length: >= 0 } ? maxLength.Length : (int?)null;
            var minimum = minLength?.Length;
            if (type == typeof(string))
            {
                if (stringLength is not null)
                {
                    maximum = maximum is null ? stringLength.MaximumLength : Math.Min(maximum.Value, stringLength.MaximumLength);
                    minimum = Math.Max(minimum ?? 0, stringLength.MinimumLength);
                }
                usage.MaxLength = maximum;
                usage.MinLength = minimum;
            }
            else if (type == typeof(byte[]))
            {
                // System.Text.Json writes bytes as base64. Length attributes constrain
                // raw bytes, so document the corresponding encoded character ceiling.
                usage.MaxLength = maximum is null ? null : checked((int)(((long)maximum.Value + 2) / 3 * 4));
                usage.MinLength = minimum is null ? null : checked((int)(((long)minimum.Value + 2) / 3 * 4));
            }
            else if (MapJsonTypeToSchemaType(type.GetTypeInfo()) == AsyncApiSchemaValueType.Array)
            {
                usage.MaxItems = maximum;
                usage.MinItems = minimum;
            }

            if (range is not null && MapJsonTypeToSchemaType(type.GetTypeInfo()) is AsyncApiSchemaValueType.Integer or AsyncApiSchemaValueType.Number)
            {
                usage.Minimum = Convert.ToDouble(range.Minimum, CultureInfo.InvariantCulture);
                usage.Maximum = Convert.ToDouble(range.Maximum, CultureInfo.InvariantCulture);
                if (!double.IsFinite(usage.Minimum.Value) || !double.IsFinite(usage.Maximum.Value))
                {
                    throw new InvalidOperationException($"Numeric range on '{property.DeclaringType}.{property.Name}' must have finite bounds.");
                }
            }
            return usage;
        }

        private static AsyncApiSchemaDescriptor CreateUsageSchema(AsyncApiSchemaDescriptor schema, bool isNullable)
        {
            if (!isNullable)
            {
                return schema;
            }

            if (!string.IsNullOrWhiteSpace(schema.Reference))
            {
                return CreateNullableReferenceWrapper(schema.Reference);
            }

            if (!string.IsNullOrWhiteSpace(schema.Id)
                && schema.Type is AsyncApiSchemaValueType.Object or AsyncApiSchemaValueType.Array)
            {
                return CreateNullableReferenceWrapper($"#/components/schemas/{schema.Id}");
            }

            if (!string.IsNullOrWhiteSpace(schema.Id))
            {
                var clone = CloneSchema(schema);
                clone.Id = null;
                clone.Nullable = true;
                return clone;
            }

            schema.Nullable = true;
            return schema;
        }

        private static AsyncApiSchemaDescriptor CreateCollectionUsageSchema(
            AsyncApiSchemaDescriptor schema,
            bool isNullable,
            bool isRoot)
        {
            if (isRoot)
            {
                return CreateUsageSchema(schema, isNullable);
            }

            if (!isNullable)
            {
                return schema;
            }

            var inlineSchema = CloneSchema(schema);
            inlineSchema.Id = null;
            inlineSchema.Nullable = true;
            return inlineSchema;
        }

        private static AsyncApiSchemaDescriptor CreateNullableReferenceWrapper(string reference)
        {
            var wrapper = new AsyncApiSchemaDescriptor
            {
                Nullable = true,
            };
            wrapper.AllOf.Add(new AsyncApiSchemaDescriptor
            {
                Reference = reference,
            });

            return wrapper;
        }

        private static AsyncApiSchemaDescriptor CloneSchema(AsyncApiSchemaDescriptor schema)
        {
            var clone = new AsyncApiSchemaDescriptor
            {
                Id = schema.Id,
                Type = schema.Type,
                Format = schema.Format,
                Description = schema.Description,
                MaxLength = schema.MaxLength,
                MinLength = schema.MinLength,
                MaxItems = schema.MaxItems,
                MinItems = schema.MinItems,
                Maximum = schema.Maximum,
                Minimum = schema.Minimum,
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

        private static bool ReferencesComponent(
            AsyncApiSchemaDescriptor? schema,
            string reference,
            HashSet<AsyncApiSchemaDescriptor> visited)
        {
            if (schema is null || !visited.Add(schema))
            {
                return false;
            }

            if (string.Equals(schema.Reference, reference, StringComparison.Ordinal)
                || ReferencesComponent(schema.Items, reference, visited)
                || ReferencesComponent(schema.AdditionalProperties, reference, visited))
            {
                return true;
            }

            return schema.Properties.Values.Any(property => ReferencesComponent(property, reference, visited))
                || schema.OneOf.Any(item => ReferencesComponent(item, reference, visited))
                || schema.AllOf.Any(item => ReferencesComponent(item, reference, visited));
        }

        private static Type? GetEnumerableItemType(TypeInfo typeInfo)
        {
            if (IsDictionaryType(typeInfo))
            {
                return null;
            }

            if (typeInfo.IsArray)
            {
                return typeInfo.GetElementType();
            }

            if (typeInfo.IsGenericType && typeInfo.GenericTypeArguments.Length == 1)
            {
                var genericType = typeInfo.GetGenericTypeDefinition();
                if (genericType == typeof(IEnumerable<>) || typeof(IEnumerable).IsAssignableFrom(typeInfo.AsType()))
                {
                    return typeInfo.GenericTypeArguments[0];
                }
            }

            var enumerableInterface = typeInfo
                .ImplementedInterfaces
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            return enumerableInterface?.GenericTypeArguments[0];
        }

        private static bool IsDictionaryType(TypeInfo typeInfo)
        {
            return TryGetDictionaryTypeArguments(typeInfo, out _, out _)
                || typeof(IDictionary).IsAssignableFrom(typeInfo.AsType());
        }

        private static bool IsGenericType(TypeInfo typeInfo, Type genericTypeDefinition)
        {
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == genericTypeDefinition;
        }

        private static bool TryGetDictionaryValueType(TypeInfo typeInfo, out Type valueType)
        {
            if (TryGetDictionaryTypeArguments(typeInfo, out var keyType, out valueType))
            {
                if (keyType == typeof(string))
                {
                    return true;
                }

                throw new InvalidOperationException(
                    $"Dictionary type '{typeInfo.AsType()}' cannot be represented as an AsyncAPI schema map because it uses non-string keys.");
            }

            if (typeof(IDictionary).IsAssignableFrom(typeInfo.AsType()))
            {
                throw new InvalidOperationException(
                    $"Dictionary type '{typeInfo.AsType()}' cannot be represented as an AsyncAPI schema map because its value type cannot be determined.");
            }

            valueType = null!;
            return false;
        }

        private static bool TryGetDictionaryTypeArguments(TypeInfo typeInfo, out Type keyType, out Type valueType)
        {
            if (TryGetDictionaryTypeArguments(typeInfo.AsType(), out keyType, out valueType))
            {
                return true;
            }

            foreach (var interfaceType in typeInfo.ImplementedInterfaces)
            {
                if (TryGetDictionaryTypeArguments(interfaceType.GetTypeInfo(), out keyType, out valueType))
                {
                    return true;
                }
            }

            keyType = null!;
            valueType = null!;
            return false;
        }

        private static bool TryGetDictionaryTypeArguments(Type type, out Type keyType, out Type valueType)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType)
            {
                var genericType = typeInfo.GetGenericTypeDefinition();
                if (genericType == typeof(IDictionary<,>) || genericType == typeof(IReadOnlyDictionary<,>))
                {
                    keyType = typeInfo.GenericTypeArguments[0];
                    valueType = typeInfo.GenericTypeArguments[1];
                    return true;
                }
            }

            keyType = null!;
            valueType = null!;
            return false;
        }

        private static IEnumerable<string> GetEnumValues(TypeInfo typeInfo)
        {
            foreach (var name in typeInfo.GetEnumNames())
            {
                var field = typeInfo.GetField(name);
                var jsonStringEnumMemberName = field?.GetCustomAttribute<JsonStringEnumMemberNameAttribute>();
                if (jsonStringEnumMemberName is not null)
                {
                    yield return jsonStringEnumMemberName.Name;
                    continue;
                }

                var enumMember = field?.GetCustomAttribute<EnumMemberAttribute>();
                yield return string.IsNullOrWhiteSpace(enumMember?.Value) ? name : enumMember.Value!;
            }
        }

        private static string ToSchemaName(TypeInfo typeInfo)
        {
            var name = typeInfo.Name;
            if (typeInfo.IsGenericType)
            {
                var tickIndex = name.IndexOf('`');
                var baseName = tickIndex >= 0 ? name[..tickIndex] : name;
                var genericSuffix = string.Concat(typeInfo.GenericTypeArguments.Select(argument => ToSchemaName(argument.GetTypeInfo())));
                name = baseName + genericSuffix;
            }

            return ToSchemaName(name, true);
        }

        private static string GetSchemaId(
            TypeInfo typeInfo,
            AsyncApiSchemaValueType? schemaType,
            SchemaGenerationContext generationContext,
            bool isRoot,
            string? collectionNullability)
        {
            var type = typeInfo.AsType();
            var key = new SchemaIdentity(type, collectionNullability);
            if (generationContext.AssignedSchemaIds.TryGetValue(key, out var existingId))
            {
                return existingId;
            }

            var schemaId = schemaType is AsyncApiSchemaValueType.Object or AsyncApiSchemaValueType.Array
                ? (isRoot ? ToSchemaName(typeInfo) : ToQualifiedSchemaName(typeInfo))
                : ToSchemaName(typeInfo);
            schemaId += collectionNullability;

            generationContext.AssignedSchemaIds[key] = schemaId;
            return schemaId;
        }

        private static string? GetCollectionNullabilityDiscriminator(
            bool isDictionary,
            AsyncApiSchemaValueType? schemaType,
            NullabilityInfo? nullabilityInfo)
        {
            NullabilityInfo? itemNullability = null;
            if (isDictionary)
            {
                itemNullability = GetDictionaryValueNullabilityInfo(nullabilityInfo);
            }
            else if (schemaType == AsyncApiSchemaValueType.Array)
            {
                itemNullability = GetItemNullabilityInfo(nullabilityInfo);
            }

            return itemNullability is not null && HasNonRequiredAnnotation(itemNullability)
                ? $"With{FormatNullability(itemNullability)}Items"
                : null;
        }

        private static bool HasNonRequiredAnnotation(NullabilityInfo nullabilityInfo)
        {
            return nullabilityInfo.ReadState != NullabilityState.NotNull
                || nullabilityInfo.ElementType is not null && HasNonRequiredAnnotation(nullabilityInfo.ElementType)
                || nullabilityInfo.GenericTypeArguments.Any(HasNonRequiredAnnotation);
        }

        private static string FormatNullability(NullabilityInfo nullabilityInfo)
        {
            var state = nullabilityInfo.ReadState switch
            {
                NullabilityState.Nullable => "Nullable",
                NullabilityState.NotNull => "Required",
                _ => "Unknown",
            };

            if (nullabilityInfo.ElementType is not null)
            {
                return $"{state}Of{FormatNullability(nullabilityInfo.ElementType)}";
            }

            return nullabilityInfo.GenericTypeArguments.Length == 0
                ? state
                : $"{state}Of{string.Join("And", nullabilityInfo.GenericTypeArguments.Select(FormatNullability))}";
        }

        private static string ToQualifiedSchemaName(TypeInfo typeInfo)
        {
            return ToSchemaName(GetQualifiedTypeName(typeInfo), true);
        }

        private static string GetQualifiedTypeName(TypeInfo typeInfo)
        {
            if (typeInfo.IsArray)
            {
                var elementType = typeInfo.GetElementType();
                return elementType is null
                    ? typeInfo.Name
                    : $"{GetQualifiedTypeName(elementType.GetTypeInfo())}Array";
            }

            var baseName = typeInfo.Name;
            if (typeInfo.IsGenericType)
            {
                var tickIndex = baseName.IndexOf('`');
                baseName = tickIndex >= 0 ? baseName[..tickIndex] : baseName;
                baseName += "Of" + string.Join("And", typeInfo.GenericTypeArguments.Select(argument => GetQualifiedTypeName(argument.GetTypeInfo())));
            }

            string? prefix = null;
            if (typeInfo.DeclaringType is not null)
            {
                prefix = GetQualifiedTypeName(typeInfo.DeclaringType.GetTypeInfo());
            }
            else if (!string.IsNullOrWhiteSpace(typeInfo.Namespace))
            {
                prefix = typeInfo.Namespace;
            }

            return string.IsNullOrWhiteSpace(prefix)
                ? baseName
                : $"{prefix}.{baseName}";
        }

        private static AsyncApiSchemaDescriptor[] DeduplicateSchemas(IEnumerable<AsyncApiSchemaDescriptor> schemas, string context)
        {
            var deduplicatedSchemas = new List<AsyncApiSchemaDescriptor>();

            foreach (var schemasById in schemas.GroupBy(schema => schema.Id, StringComparer.Ordinal))
            {
                var representative = schemasById.First();
                foreach (var candidate in schemasById.Skip(1))
                {
                    if (!SchemaDescriptorsMatch(representative, candidate))
                    {
                        throw new InvalidOperationException(
                            $"Conflicting schema descriptors found for id '{schemasById.Key}' while {context}. " +
                            $"Existing definition: {FormatSchemaDescriptor(representative)}. Incoming definition: {FormatSchemaDescriptor(candidate)}.");
                    }
                }

                deduplicatedSchemas.Add(representative);
            }

            return deduplicatedSchemas.ToArray();
        }

        private static string ToSchemaName(string name, bool camelCase)
        {
            var sanitized = new string(name
                .Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_' ? ch : '_')
                .ToArray());

            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = "schema";
            }

            if (!camelCase || sanitized.Length == 0)
            {
                return sanitized;
            }

            return char.ToLowerInvariant(sanitized[0]) + sanitized[1..];
        }

        private static bool IsNullable(TypeInfo typeInfo, NullabilityInfo? nullabilityInfo, bool isRoot)
        {
            if (Nullable.GetUnderlyingType(typeInfo.AsType()) is not null)
            {
                return true;
            }

            if (typeInfo.IsValueType)
            {
                return false;
            }

            if (isRoot && nullabilityInfo is null)
            {
                return false;
            }

            return nullabilityInfo?.ReadState switch
            {
                NullabilityState.Nullable => true,
                NullabilityState.NotNull => false,
                _ => true,
            };
        }

        private static bool IsRequiredProperty(PropertyInfo propertyInfo, NullabilityInfo nullabilityInfo)
        {
            if (!propertyInfo.CanRead)
            {
                return false;
            }

            if (Nullable.GetUnderlyingType(propertyInfo.PropertyType) is not null)
            {
                return false;
            }

            var propertyTypeInfo = propertyInfo.PropertyType.GetTypeInfo();
            if (propertyTypeInfo.IsValueType)
            {
                return true;
            }

            return nullabilityInfo.ReadState == NullabilityState.NotNull;
        }

        private static NullabilityInfo? GetItemNullabilityInfo(NullabilityInfo? nullabilityInfo)
        {
            if (nullabilityInfo is null)
            {
                return null;
            }

            if (nullabilityInfo.ElementType is not null)
            {
                return nullabilityInfo.ElementType;
            }

            return nullabilityInfo.GenericTypeArguments.FirstOrDefault();
        }

        private static NullabilityInfo? GetDictionaryValueNullabilityInfo(NullabilityInfo? nullabilityInfo)
        {
            if (nullabilityInfo is null)
            {
                return null;
            }

            var genericTypeArguments = nullabilityInfo.GenericTypeArguments;
            if (genericTypeArguments.Length < 2)
            {
                return null;
            }

            return genericTypeArguments[1];
        }

        private static bool SchemaDescriptorsMatch(AsyncApiSchemaDescriptor source, AsyncApiSchemaDescriptor additional)
        {
            if (!string.Equals(source.Id, additional.Id, StringComparison.Ordinal)
                || source.Type != additional.Type
                || !string.Equals(source.Format, additional.Format, StringComparison.Ordinal)
                || source.Nullable != additional.Nullable
                || source.Description != additional.Description
                || source.MaxLength != additional.MaxLength
                || source.MinLength != additional.MinLength
                || source.MaxItems != additional.MaxItems
                || source.MinItems != additional.MinItems
                || source.Maximum != additional.Maximum
                || source.Minimum != additional.Minimum
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

        private static string FormatSchemaDescriptor(AsyncApiSchemaDescriptor schema)
        {
            return $"id={FormatValue(schema.Id)}, type={schema.Type?.ToString() ?? "<null>"}, format={FormatValue(schema.Format)}, nullable={schema.Nullable}, reference={FormatValue(schema.Reference)}, properties={FormatValues(schema.Properties.Keys)}, oneOfCount={schema.OneOf.Count}, allOfCount={schema.AllOf.Count}";
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

        private static readonly TypeInfo s_boolTypeInfo = typeof(bool).GetTypeInfo();

        private static readonly TypeInfo[] s_stringTypeInfos =
        {
            typeof(string).GetTypeInfo(),
            typeof(DateTime).GetTypeInfo(),
            typeof(DateTimeOffset).GetTypeInfo(),
            typeof(TimeSpan).GetTypeInfo(),
            typeof(Guid).GetTypeInfo(),
            typeof(Uri).GetTypeInfo(),
            typeof(DateOnly).GetTypeInfo(),
            typeof(TimeOnly).GetTypeInfo(),
        };

        private static readonly TypeInfo[] s_integerTypeInfos =
        {
            typeof(byte).GetTypeInfo(),
            typeof(short).GetTypeInfo(),
            typeof(int).GetTypeInfo(),
            typeof(long).GetTypeInfo(),
            typeof(uint).GetTypeInfo(),
            typeof(ushort).GetTypeInfo(),
            typeof(ulong).GetTypeInfo(),
        };

        private static readonly TypeInfo[] s_floatTypeInfos =
        {
            typeof(float).GetTypeInfo(),
            typeof(decimal).GetTypeInfo(),
            typeof(double).GetTypeInfo(),
        };

        private static AsyncApiSchemaValueType? MapJsonTypeToSchemaType(TypeInfo typeInfo)
        {
            if (typeInfo == s_boolTypeInfo)
            {
                return AsyncApiSchemaValueType.Boolean;
            }

            if (typeInfo.IsEnum)
            {
                return AsyncApiSchemaValueType.String;
            }

            if (s_stringTypeInfos.Contains(typeInfo))
            {
                return AsyncApiSchemaValueType.String;
            }

            if (s_integerTypeInfos.Contains(typeInfo))
            {
                return AsyncApiSchemaValueType.Integer;
            }

            if (s_floatTypeInfos.Contains(typeInfo))
            {
                return AsyncApiSchemaValueType.Number;
            }

            // byte[] serializes as a base64 string in System.Text.Json, not a JSON array.
            if (typeInfo.AsType() == typeof(byte[]))
            {
                return AsyncApiSchemaValueType.String;
            }

            if (typeInfo.IsArray || GetEnumerableItemType(typeInfo) is not null && typeInfo.AsType() != typeof(string))
            {
                return AsyncApiSchemaValueType.Array;
            }

            return AsyncApiSchemaValueType.Object;
        }

        private sealed class SchemaGenerationContext
        {
            public IDictionary<SchemaIdentity, string> AssignedSchemaIds { get; } = new Dictionary<SchemaIdentity, string>();

            public ISet<string> ReusableCollectionSchemaIds { get; } = new HashSet<string>(StringComparer.Ordinal);
        }

        private readonly record struct SchemaIdentity(Type Type, string? CollectionNullability);
    }
}
