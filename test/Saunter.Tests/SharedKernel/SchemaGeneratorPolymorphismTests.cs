using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Saunter.SharedKernel;
using Saunter.SharedKernel.Descriptors;
using Shouldly;
using Xunit;

#nullable enable

namespace Saunter.Tests.SharedKernel
{
    public class SchemaGeneratorPolymorphismTests
    {
        [Fact]
        public void ExportsEveryDeclaredEffectWithItsWireDiscriminatorAndInheritedProperties()
        {
            var generated = new AsyncApiSchemaGenerator().Generate(typeof(MemberBenefits))!.Value;
            var effect = generated.All.Single(schema => schema.Id!.EndsWith(".Effect"));
            effect.OneOf.Count.ShouldBe(2);
            var tierBranch = effect.OneOf.Single(branch => branch.AllOf[1].Properties["$type"].EnumValues.Contains("loyaltyTier"));
            tierBranch.AllOf[1].Required.ShouldContain("$type");
            var tier = generated.All.Single(schema => "#/components/schemas/" + schema.Id == tierBranch.AllOf[0].Reference);
            tier.Properties.Keys.ShouldContain("tierName");
            tier.Properties.Keys.ShouldContain("backendId");
            tier.Properties.Keys.ShouldContain("sourceBenefitId");
            tier.Properties.Keys.ShouldNotContain("$type");
            JsonSerializer.Serialize<Effect>(new TierEffect { TierName = "Gold" }).ShouldContain("\"$type\":\"loyaltyTier\"");
        }

        [Fact]
        public void KeepsNullableAndRecursiveEffectReferencesAndDistinctConcreteUsage()
        {
            var generated = new AsyncApiSchemaGenerator().Generate(typeof(MemberBenefits))!.Value;
            var optional = generated.Root.Properties["optional"];
            optional.Nullable.ShouldBeTrue();
            var baseReference = optional.AllOf.Single().Reference;
            generated.All.ShouldContain(schema => "#/components/schemas/" + schema.Id == baseReference && schema.OneOf.Count == 2);
            var nested = generated.All.Single(schema => schema.Id!.EndsWith(".NestedEffect"));
            nested.Properties["child"].AllOf.Single().Reference.ShouldBe(baseReference);
            generated.All.Select(schema => schema.Id).ShouldBeUnique();
            new AsyncApiSchemaGenerator().Generate(typeof(TierEffect))!.Value.Root.Properties.Keys.ShouldNotContain("$type");
        }

        [Fact]
        public void UsesCustomDiscriminatorName()
        {
            var generated = new AsyncApiSchemaGenerator().Generate(typeof(CustomEffect))!.Value;
            generated.Root.OneOf.Single().AllOf[1].Properties["kind"].EnumValues.ShouldBe(new[] { "custom" });
        }

        [Fact]
        public void RejectsUnsupportedDiscriminatorsInsteadOfExportingAnIncompleteContract()
        {
            Should.Throw<System.InvalidOperationException>(() => new AsyncApiSchemaGenerator().Generate(typeof(NumericEffect)))
                .Message.ShouldContain("string discriminator");
        }

        [Theory]
        [InlineData(typeof(UntaggedEffect), "string discriminator")]
        [InlineData(typeof(ConcreteEffect), "abstract or an interface")]
        [InlineData(typeof(DuplicateEffect), "distinct discriminators")]
        [InlineData(typeof(CollidingEffect), "conflicts with a serialized property")]
        [InlineData(typeof(CollidingTier), "conflicts with a serialized property")]
        public void RejectsUnsupportedShapesEvenWhenTheConcreteTypeIsTheRoot(System.Type type, string expectedMessage)
        {
            Should.Throw<System.InvalidOperationException>(() => new AsyncApiSchemaGenerator().Generate(type))
                .Message.ShouldContain(expectedMessage);
        }

        public sealed class MemberBenefits
        {
            public IReadOnlyList<Effect> Benefits { get; init; } = [];
            public Effect? Optional { get; init; }
            public TierEffect? Concrete { get; init; }
        }

        [JsonPolymorphic]
        [JsonDerivedType(typeof(TierEffect), "loyaltyTier")]
        [JsonDerivedType(typeof(NestedEffect), "nested")]
        public abstract class Effect
        {
            public string BackendId { get; init; } = "partner";
            public string SourceBenefitId { get; init; } = "gold";
        }

        public sealed class TierEffect : Effect
        {
            public string TierName { get; init; } = "";
        }

        public sealed class NestedEffect : Effect
        {
            public Effect? Child { get; init; }
        }

        [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
        [JsonDerivedType(typeof(CustomTier), "custom")]
        public abstract class CustomEffect { }
        public sealed class CustomTier : CustomEffect { }

        [JsonDerivedType(typeof(UntaggedTier))]
        public abstract class UntaggedEffect { }
        public sealed class UntaggedTier : UntaggedEffect { }

        [JsonDerivedType(typeof(ConcreteTier), "tier")]
        public class ConcreteEffect { }
        public sealed class ConcreteTier : ConcreteEffect { }

        [JsonDerivedType(typeof(DuplicateTier), "same")]
        [JsonDerivedType(typeof(OtherDuplicateTier), "same")]
        public abstract class DuplicateEffect { }
        public sealed class DuplicateTier : DuplicateEffect { }
        public sealed class OtherDuplicateTier : DuplicateEffect { }

        [JsonDerivedType(typeof(CollidingTier), "tier")]
        public abstract class CollidingEffect { }
        public sealed class CollidingTier : CollidingEffect
        {
            [JsonPropertyName("$type")]
            public string Type { get; init; } = "tier";
            public CollidingEffect? Child { get; init; }
        }

        [JsonDerivedType(typeof(NumericTier), 1)]
        public abstract class NumericEffect { }
        public sealed class NumericTier : NumericEffect { }
    }
}
