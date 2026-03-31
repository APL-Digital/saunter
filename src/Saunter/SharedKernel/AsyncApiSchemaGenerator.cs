using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ByteBard.AsyncAPI.Models;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.SharedKernel
{
    internal class AsyncApiSchemaGenerator : IAsyncApiSchemaGenerator
    {
        private static readonly NullabilityInfoContext s_nullabilityInfoContext = new();

        public GeneratedSchemas? Generate(Type? type)
        {
            var generatedSchemas = GenerateBranch(type, new HashSet<Type>());
            if (generatedSchemas is null)
            {
                return null;
            }

            var allSchemas = new List<AsyncApiJsonSchema>();
            if (!string.IsNullOrWhiteSpace(generatedSchemas.Value.Root.Title))
            {
                allSchemas.Add(generatedSchemas.Value.Root);
            }

            allSchemas.AddRange(generatedSchemas.Value.All);

            return new(
                generatedSchemas.Value.Root,
                allSchemas
                    .Where(schema => !string.IsNullOrWhiteSpace(schema.Title))
                    .DistinctBy(schema => schema.Title)
                    .ToArray());
        }

        private static GeneratedSchemas? GenerateBranch(Type? type, HashSet<Type> parents, NullabilityInfo? nullabilityInfo = null)
        {
            if (type is null)
            {
                return null;
            }

            var typeInfo = type.GetTypeInfo();
            var isNullable = IsNullable(typeInfo, nullabilityInfo);

            if (Nullable.GetUnderlyingType(type) is Type underlyingType)
            {
                type = underlyingType;
                typeInfo = type.GetTypeInfo();
                isNullable = true;
            }

            var schema = new AsyncApiJsonSchema
            {
                Nullable = isNullable,
            };

            var name = ToNameCase(typeInfo.Name);

            schema.Title = name;
            schema.Type = MapJsonTypeToSchemaType(typeInfo);

            if (schema.Type is not SchemaType.Object and not SchemaType.Array)
            {
                if (typeInfo.IsEnum)
                {
                    schema.Format = "enum";
                    schema.Enum = typeInfo
                        .GetEnumNames()
                        .Select(value => new AsyncApiAny(value))
                        .ToList();
                }
                else
                {
                    schema.Format = schema.Title;
                }

                return new(schema, Array.Empty<AsyncApiJsonSchema>());
            }

            if (schema.Type == SchemaType.Array)
            {
                var itemSchemas = new List<AsyncApiJsonSchema>();
                var itemType = GetEnumerableItemType(typeInfo);
                var generatedItemSchema = GenerateBranch(itemType, parents, GetItemNullabilityInfo(nullabilityInfo));
                if (generatedItemSchema is not null)
                {
                    schema.Items = generatedItemSchema.Value.Root;
                    itemSchemas.AddRange(generatedItemSchema.Value.All);
                }

                return new(schema, itemSchemas.DistinctBy(n => n.Title).ToArray());
            }

            if (!parents.Add(type))
            {
                var referenceSchema = new AsyncApiJsonSchemaReference($"#/components/schemas/{name}");
                if (!isNullable)
                {
                    return new(referenceSchema, Array.Empty<AsyncApiJsonSchema>());
                }

                var nullableReferenceSchema = new AsyncApiJsonSchema
                {
                    Nullable = true,
                };
                nullableReferenceSchema.AllOf.Add(referenceSchema);

                return new(nullableReferenceSchema, Array.Empty<AsyncApiJsonSchema>());
            }

            var nestedSchemas = new List<AsyncApiJsonSchema> { schema };
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

                var propertyName = ToNameCase(prop.Name);
                schema.Properties[propertyName] = generatedSchemas.Value.Root;
                if (IsRequiredProperty(prop, propertyNullability))
                {
                    schema.Required.Add(propertyName);
                }

                nestedSchemas.AddRange(generatedSchemas.Value.All);
            }

            return new(schema, nestedSchemas.DistinctBy(n => n.Title).ToArray());
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

        private static string ToNameCase(string name)
        {
            return char.ToLowerInvariant(name[0]) + name[1..];
        }

        private static bool IsNullable(TypeInfo typeInfo, NullabilityInfo? nullabilityInfo)
        {
            if (Nullable.GetUnderlyingType(typeInfo.AsType()) is not null)
            {
                return true;
            }

            if (typeInfo.IsValueType)
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

        private static SchemaType? MapJsonTypeToSchemaType(TypeInfo typeInfo)
        {
            if (typeInfo == s_boolTypeInfo)
            {
                return SchemaType.Boolean;
            }

            if (typeInfo.IsEnum)
            {
                return SchemaType.String;
            }

            if (s_stringTypeInfos.Contains(typeInfo))
            {
                return SchemaType.String;
            }

            if (s_integerTypeInfos.Contains(typeInfo))
            {
                return SchemaType.Integer;
            }

            if (s_floatTypeInfos.Contains(typeInfo))
            {
                return SchemaType.Number;
            }

            if (typeInfo.IsArray || GetEnumerableItemType(typeInfo) is not null && typeInfo.AsType() != typeof(string))
            {
                return SchemaType.Array;
            }

            return SchemaType.Object;
        }
    }
}
