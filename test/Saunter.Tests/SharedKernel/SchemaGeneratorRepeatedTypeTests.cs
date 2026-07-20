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
            requiredValues.AdditionalProperties.ShouldNotBeNull();
            requiredValues.AdditionalProperties.Nullable.ShouldBeFalse();
            nullableValues.AdditionalProperties.ShouldNotBeNull();
            nullableValues.AdditionalProperties.Nullable.ShouldBeTrue();
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
    }
}
