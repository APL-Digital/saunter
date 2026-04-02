using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Saunter.SharedKernel.Descriptors;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.SharedKernel
{
    internal class AsyncApiSchemaGenerator : IAsyncApiSchemaGenerator
    {
        public GeneratedSchemaDescriptors? Generate(Type? type)
        {
            var nullabilityInfoContext = new NullabilityInfoContext();
            var generatedSchemas = GenerateBranch(type, new HashSet<Type>(), nullabilityInfoContext, isRoot: true);
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

        private static GeneratedSchemaDescriptors? GenerateBranch(Type? type, HashSet<Type> parents, NullabilityInfoContext nullabilityInfoContext, NullabilityInfo? nullabilityInfo = null, bool isRoot = false)
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

            var name = ToSchemaName(typeInfo);
            var schema = new AsyncApiSchemaDescriptor
            {
                Id = name,
                Type = MapJsonTypeToSchemaType(typeInfo),
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
                var generatedItemSchema = GenerateBranch(itemType, parents, nullabilityInfoContext, GetItemNullabilityInfo(nullabilityInfo));
                if (generatedItemSchema is not null)
                {
                    schema.Items = generatedItemSchema.Value.Root;
                    itemSchemas.AddRange(generatedItemSchema.Value.All);
                }

                var usageSchema = CreateUsageSchema(schema, isNullable);
                if (!ReferenceEquals(usageSchema, schema))
                {
                    itemSchemas.Insert(0, schema);
                }

                return new(usageSchema, DeduplicateSchemas(itemSchemas, $"building array items for schema '{name}'"));
            }

            if (TryGetDictionaryValueType(typeInfo, out var dictionaryValueType))
            {
                if (!parents.Add(type))
                {
                    var referenceSchema = new AsyncApiSchemaDescriptor
                    {
                        Reference = $"#/components/schemas/{name}",
                    };
                    return new(CreateUsageSchema(referenceSchema, isNullable), Array.Empty<AsyncApiSchemaDescriptor>());
                }

                var dictionarySchemas = new List<AsyncApiSchemaDescriptor> { schema };
                var generatedValueSchema = GenerateBranch(dictionaryValueType, parents, nullabilityInfoContext, GetDictionaryValueNullabilityInfo(nullabilityInfo));
                if (generatedValueSchema is not null)
                {
                    schema.AdditionalProperties = generatedValueSchema.Value.Root;
                    dictionarySchemas.AddRange(generatedValueSchema.Value.All);
                }

                return new(CreateUsageSchema(schema, isNullable), DeduplicateSchemas(dictionarySchemas, $"building dictionary values for schema '{name}'"));
            }

            if (!parents.Add(type))
            {
                var referenceSchema = new AsyncApiSchemaDescriptor
                {
                    Reference = $"#/components/schemas/{name}",
                };
                return new(CreateUsageSchema(referenceSchema, isNullable), Array.Empty<AsyncApiSchemaDescriptor>());
            }

            var nestedSchemas = new List<AsyncApiSchemaDescriptor> { schema };
            var properties = typeInfo.AsType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetMethod is not null && !p.GetMethod.IsStatic && p.GetIndexParameters().Length == 0);

            foreach (var prop in properties)
            {
                var propertyNullability = nullabilityInfoContext.Create(prop);
                var generatedSchemas = GenerateBranch(prop.PropertyType, parents, nullabilityInfoContext, propertyNullability);
                if (generatedSchemas is null)
                {
                    continue;
                }

                var propertyName = ToSchemaName(prop.Name, true);
                schema.Properties[propertyName] = generatedSchemas.Value.Root;
                if (IsRequiredProperty(prop, propertyNullability))
                {
                    schema.Required.Add(propertyName);
                }

                nestedSchemas.AddRange(generatedSchemas.Value.All);
            }

            return new(CreateUsageSchema(schema, isNullable), DeduplicateSchemas(nestedSchemas, $"building object properties for schema '{name}'"));
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

            if (typeInfo.IsArray || GetEnumerableItemType(typeInfo) is not null && typeInfo.AsType() != typeof(string))
            {
                return AsyncApiSchemaValueType.Array;
            }

            return AsyncApiSchemaValueType.Object;
        }
    }
}
