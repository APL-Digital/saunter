using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Saunter.SharedKernel.Descriptors;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.SharedKernel
{
    internal class AsyncApiSchemaGenerator : IAsyncApiSchemaGenerator
    {
        private static readonly NullabilityInfoContext s_nullabilityInfoContext = new();

        public GeneratedSchemaDescriptors? Generate(Type? type)
        {
            var generatedSchemas = GenerateBranch(type, new HashSet<Type>(), isRoot: true);
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
                allSchemas
                    .Where(schema => !string.IsNullOrWhiteSpace(schema.Id))
                    .DistinctBy(schema => schema.Id)
                    .ToArray());
        }

        private static GeneratedSchemaDescriptors? GenerateBranch(Type? type, HashSet<Type> parents, NullabilityInfo? nullabilityInfo = null, bool isRoot = false)
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
                Nullable = isNullable,
                Id = name,
                Type = MapJsonTypeToSchemaType(typeInfo),
            };

            if (schema.Type is not AsyncApiSchemaValueType.Object and not AsyncApiSchemaValueType.Array)
            {
                if (typeInfo.IsEnum)
                {
                    schema.Format = "enum";
                    foreach (var value in typeInfo.GetEnumNames())
                    {
                        schema.EnumValues.Add(value);
                    }
                }
                else
                {
                    schema.Format = name;
                }

                return new(schema, Array.Empty<AsyncApiSchemaDescriptor>());
            }

            if (schema.Type == AsyncApiSchemaValueType.Array)
            {
                var itemSchemas = new List<AsyncApiSchemaDescriptor>();
                var itemType = GetEnumerableItemType(typeInfo);
                var generatedItemSchema = GenerateBranch(itemType, parents, GetItemNullabilityInfo(nullabilityInfo));
                if (generatedItemSchema is not null)
                {
                    schema.Items = generatedItemSchema.Value.Root;
                    itemSchemas.AddRange(generatedItemSchema.Value.All);
                }

                return new(schema, itemSchemas.DistinctBy(n => n.Id).ToArray());
            }

            if (!parents.Add(type))
            {
                var referenceSchema = new AsyncApiSchemaDescriptor
                {
                    Reference = $"#/components/schemas/{name}",
                };
                if (!isNullable)
                {
                    return new(referenceSchema, Array.Empty<AsyncApiSchemaDescriptor>());
                }

                var nullableReferenceSchema = new AsyncApiSchemaDescriptor
                {
                    Nullable = true,
                };
                nullableReferenceSchema.AllOf.Add(referenceSchema);

                return new(nullableReferenceSchema, Array.Empty<AsyncApiSchemaDescriptor>());
            }

            var nestedSchemas = new List<AsyncApiSchemaDescriptor> { schema };
            var properties = typeInfo
                .DeclaredProperties
                .Where(p => p.GetMethod is not null && !p.GetMethod.IsStatic);

            foreach (var prop in properties)
            {
                var propertyNullability = s_nullabilityInfoContext.Create(prop);
                var generatedSchemas = GenerateBranch(prop.PropertyType, parents, propertyNullability);
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

            return new(schema, nestedSchemas.DistinctBy(n => n.Id).ToArray());
        }

        private static Type? GetEnumerableItemType(TypeInfo typeInfo)
        {
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
