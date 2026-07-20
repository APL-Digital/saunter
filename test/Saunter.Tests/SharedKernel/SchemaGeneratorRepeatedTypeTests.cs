using System.Collections.Generic;
using System.Linq;
#nullable enable
using Saunter.SharedKernel;
using Shouldly;
using Xunit;

namespace Saunter.Tests.SharedKernel
{
    public class SchemaGeneratorRepeatedTypeTests
    {
        [Fact]
        public void AsyncApiSchemaGenerator_DoesNotTreatRepeatedSiblingDictionaryTypeAsRecursion()
        {
            AsyncApiSchemaGenerator generator = new();

            var withSiblingSchema = generator.Generate(typeof(RootWithSiblingDictionary));
            var withoutSiblingSchema = generator.Generate(typeof(RootWithoutSiblingDictionary));

            withSiblingSchema.ShouldNotBeNull();
            withoutSiblingSchema.ShouldNotBeNull();

            var withSiblingWrapper = withSiblingSchema.Value.All.Single(schema => schema.Id?.EndsWith("DictionaryReuseWrapper") == true);
            var withoutSiblingWrapper = withoutSiblingSchema.Value.All.Single(schema => schema.Id?.EndsWith("DictionaryReuseWrapper") == true);

            AssertDictionaryProperty(withSiblingWrapper.Properties["name"]);
            AssertDictionaryProperty(withoutSiblingWrapper.Properties["name"]);
        }

        [Fact]
        public void AsyncApiSchemaGenerator_AllowsRepeatedDictionaryTypeWithDifferentValueNullability()
        {
            AsyncApiSchemaGenerator generator = new();

            var generated = generator.Generate(typeof(RootWithDifferentlyAnnotatedDictionaries));

            generated.ShouldNotBeNull();
            var requiredValues = generated.Value.Root.Properties["requiredValues"];
            var nullableValues = generated.Value.Root.Properties["nullableValues"];
            var optionalValues = generated.Value.Root.Properties["optionalValues"];
            requiredValues.AdditionalProperties.ShouldNotBeNull();
            requiredValues.AdditionalProperties.Nullable.ShouldBeFalse();
            nullableValues.AdditionalProperties.ShouldNotBeNull();
            nullableValues.AdditionalProperties.Nullable.ShouldBeTrue();
            optionalValues.Nullable.ShouldBeTrue();
            optionalValues.Id.ShouldBeNull();
            optionalValues.AdditionalProperties.ShouldNotBeNull();
        }

        [Fact]
        public void AsyncApiSchemaGenerator_RegistersRecursiveDictionaryComponents()
        {
            AsyncApiSchemaGenerator generator = new();

            var generated = generator.Generate(typeof(RootWithRecursiveDictionary));

            generated.ShouldNotBeNull();
            var node = generated.Value.Root.Properties["children"].AdditionalProperties.ShouldNotBeNull();
            var recursiveDictionary = node.Properties["children"];
            var reference = recursiveDictionary.Reference
                ?? recursiveDictionary.AllOf.Single().Reference.ShouldNotBeNull();
            var referencedId = reference.Split('/').Last();
            generated.Value.All.ShouldContain(schema => schema.Id == referencedId);
        }

        [Fact]
        public void AsyncApiSchemaGenerator_RegistersRecursiveDictionaryComponentsThroughNullableValues()
        {
            AsyncApiSchemaGenerator generator = new();

            var generated = generator.Generate(typeof(RootWithNullableRecursiveDictionary));

            generated.ShouldNotBeNull();
            var nullableNode = generated.Value.Root.Properties["children"].AdditionalProperties.ShouldNotBeNull();
            var nodeReference = nullableNode.AllOf.Single().Reference.ShouldNotBeNull();
            var nodeId = nodeReference.Split('/').Last();
            var node = generated.Value.All.Single(schema => schema.Id == nodeId);
            var recursiveDictionary = node.Properties["children"];
            var dictionaryReference = recursiveDictionary.Reference
                ?? recursiveDictionary.AllOf.Single().Reference.ShouldNotBeNull();
            var dictionaryId = dictionaryReference.Split('/').Last();
            generated.Value.All.ShouldContain(schema => schema.Id == dictionaryId);
        }

        [Fact]
        public void AsyncApiSchemaGenerator_DistinguishesRecursiveDictionaryValueNullability()
        {
            AsyncApiSchemaGenerator generator = new();

            var generated = generator.Generate(typeof(RootWithMixedRecursiveDictionaryNullability));

            generated.ShouldNotBeNull();
            var required = generated.Value.Root.Properties["required"];
            var nullable = generated.Value.Root.Properties["nullable"];
            required.AdditionalProperties.ShouldNotBeNull().Nullable.ShouldBeFalse();
            nullable.AdditionalProperties.ShouldNotBeNull().Nullable.ShouldBeTrue();
            generated.Value.All.Select(schema => schema.Id).ShouldBeUnique();
        }

        [Fact]
        public void AsyncApiSchemaGenerator_DistinguishesUnknownRecursiveDictionaryValueNullability()
        {
            AsyncApiSchemaGenerator generator = new();

            var generated = generator.Generate(typeof(RootWithUnknownRecursiveDictionaryNullability));

            generated.ShouldNotBeNull();
            var required = generated.Value.Root.Properties["required"];
            var unknown = generated.Value.Root.Properties["unknown"];
            required.AdditionalProperties.ShouldNotBeNull().Nullable.ShouldBeFalse();
            unknown.AdditionalProperties.ShouldNotBeNull().Nullable.ShouldBeTrue();
            generated.Value.All.Select(schema => schema.Id).ShouldBeUnique();
        }

        private static void AssertDictionaryProperty(global::Saunter.SharedKernel.Descriptors.AsyncApiSchemaDescriptor schema)
        {
            schema.Reference.ShouldBeNull();
            schema.Type.ShouldBe(global::Saunter.SharedKernel.Descriptors.AsyncApiSchemaValueType.Object);
            schema.AdditionalProperties.ShouldNotBeNull();
            schema.AdditionalProperties.Type.ShouldBe(global::Saunter.SharedKernel.Descriptors.AsyncApiSchemaValueType.String);
            schema.AdditionalProperties.Format.ShouldBe("string");
        }
    }

    public class RootWithSiblingDictionary
    {
        public Dictionary<string, string> Attributes { get; set; } = new();

        public DictionaryReuseWrapper Wrapper { get; set; } = new();
    }

    public class RootWithoutSiblingDictionary
    {
        public DictionaryReuseWrapper Wrapper { get; set; } = new();
    }

    public class DictionaryReuseWrapper
    {
        public Dictionary<string, string> Name { get; set; } = new();
    }

    public class RootWithDifferentlyAnnotatedDictionaries
    {
        public Dictionary<string, string> RequiredValues { get; set; } = new();

        public Dictionary<string, string?> NullableValues { get; set; } = new();

        public Dictionary<string, string>? OptionalValues { get; set; }
    }

    public class RootWithRecursiveDictionary
    {
        public Dictionary<string, RecursiveDictionaryNode> Children { get; set; } = new();
    }

    public class RecursiveDictionaryNode
    {
        public Dictionary<string, RecursiveDictionaryNode> Children { get; set; } = new();
    }

    public class RootWithNullableRecursiveDictionary
    {
        public Dictionary<string, NullableRecursiveDictionaryNode?> Children { get; set; } = new();
    }

    public class NullableRecursiveDictionaryNode
    {
        public Dictionary<string, NullableRecursiveDictionaryNode?> Children { get; set; } = new();
    }

    public class RootWithMixedRecursiveDictionaryNullability
    {
        public Dictionary<string, MixedRecursiveDictionaryNode> Required { get; set; } = new();

        public Dictionary<string, MixedRecursiveDictionaryNode?> Nullable { get; set; } = new();
    }

    public class MixedRecursiveDictionaryNode
    {
        public Dictionary<string, MixedRecursiveDictionaryNode> Children { get; set; } = new();
    }

    public class RootWithUnknownRecursiveDictionaryNullability
    {
        public Dictionary<string, UnknownRecursiveDictionaryNode> Required { get; set; } = new();

#nullable disable
        public Dictionary<string, UnknownRecursiveDictionaryNode> Unknown { get; set; } = new();
#nullable enable
    }

    public class UnknownRecursiveDictionaryNode
    {
        public Dictionary<string, UnknownRecursiveDictionaryNode> Children { get; set; } = new();
    }
}
