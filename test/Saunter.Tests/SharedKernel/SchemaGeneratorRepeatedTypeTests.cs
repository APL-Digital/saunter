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
}
