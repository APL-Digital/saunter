using System;
#nullable enable
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Saunter.SharedKernel;
using Saunter.SharedKernel.Descriptors;
using Shouldly;
using Xunit;

namespace Saunter.Tests.SharedKernel
{
    public class SchemaGeneratorTests
    {
        [Theory]
        [InlineData(typeof(bool), "boolean", (int)AsyncApiSchemaValueType.Boolean, false, 1)]
        [InlineData(typeof(byte), "byte", (int)AsyncApiSchemaValueType.Integer, false, 1)]
        [InlineData(typeof(short), "int16", (int)AsyncApiSchemaValueType.Integer, false, 1)]
        [InlineData(typeof(ushort), "uInt16", (int)AsyncApiSchemaValueType.Integer, false, 1)]
        [InlineData(typeof(int), "int32", (int)AsyncApiSchemaValueType.Integer, false, 1)]
        [InlineData(typeof(uint), "uInt32", (int)AsyncApiSchemaValueType.Integer, false, 1)]
        [InlineData(typeof(long), "int64", (int)AsyncApiSchemaValueType.Integer, false, 1)]
        [InlineData(typeof(ulong), "uInt64", (int)AsyncApiSchemaValueType.Integer, false, 1)]
        [InlineData(typeof(decimal), "decimal", (int)AsyncApiSchemaValueType.Number, false, 1)]
        [InlineData(typeof(float), "single", (int)AsyncApiSchemaValueType.Number, false, 1)]
        [InlineData(typeof(double), "double", (int)AsyncApiSchemaValueType.Number, false, 1)]
        [InlineData(typeof(bool?), "boolean", (int)AsyncApiSchemaValueType.Boolean, true, 1)]
        [InlineData(typeof(byte?), "byte", (int)AsyncApiSchemaValueType.Integer, true, 1)]
        [InlineData(typeof(short?), "int16", (int)AsyncApiSchemaValueType.Integer, true, 1)]
        [InlineData(typeof(ushort?), "uInt16", (int)AsyncApiSchemaValueType.Integer, true, 1)]
        [InlineData(typeof(int?), "int32", (int)AsyncApiSchemaValueType.Integer, true, 1)]
        [InlineData(typeof(uint?), "uInt32", (int)AsyncApiSchemaValueType.Integer, true, 1)]
        [InlineData(typeof(long?), "int64", (int)AsyncApiSchemaValueType.Integer, true, 1)]
        [InlineData(typeof(ulong?), "uInt64", (int)AsyncApiSchemaValueType.Integer, true, 1)]
        [InlineData(typeof(decimal?), "decimal", (int)AsyncApiSchemaValueType.Number, true, 1)]
        [InlineData(typeof(float?), "single", (int)AsyncApiSchemaValueType.Number, true, 1)]
        [InlineData(typeof(double?), "double", (int)AsyncApiSchemaValueType.Number, true, 1)]
        [InlineData(typeof(string), "string", (int)AsyncApiSchemaValueType.String, false, 1)]
        [InlineData(typeof(DateTime), "dateTime", (int)AsyncApiSchemaValueType.String, false, 1)]
        [InlineData(typeof(DateTimeOffset), "dateTimeOffset", (int)AsyncApiSchemaValueType.String, false, 1)]
        [InlineData(typeof(DateOnly), "dateOnly", (int)AsyncApiSchemaValueType.String, false, 1)]
        [InlineData(typeof(TimeOnly), "timeOnly", (int)AsyncApiSchemaValueType.String, false, 1)]
        [InlineData(typeof(TimeSpan), "timeSpan", (int)AsyncApiSchemaValueType.String, false, 1)]
        [InlineData(typeof(Guid), "guid", (int)AsyncApiSchemaValueType.String, false, 1)]
        [InlineData(typeof(DateTime?), "dateTime", (int)AsyncApiSchemaValueType.String, true, 1)]
        [InlineData(typeof(DateTimeOffset?), "dateTimeOffset", (int)AsyncApiSchemaValueType.String, true, 1)]
        [InlineData(typeof(DateOnly?), "dateOnly", (int)AsyncApiSchemaValueType.String, true, 1)]
        [InlineData(typeof(TimeOnly?), "timeOnly", (int)AsyncApiSchemaValueType.String, true, 1)]
        [InlineData(typeof(TimeSpan?), "timeSpan", (int)AsyncApiSchemaValueType.String, true, 1)]
        [InlineData(typeof(Guid?), "guid", (int)AsyncApiSchemaValueType.String, true, 1)]
        [InlineData(typeof(Uri), "uri", (int)AsyncApiSchemaValueType.String, false, 1)]
        [InlineData(typeof(object), null, (int)AsyncApiSchemaValueType.Object, false, 1)]
        [InlineData(typeof(int[]), null, (int)AsyncApiSchemaValueType.Array, false, 1)]
        [InlineData(typeof(object[]), null, (int)AsyncApiSchemaValueType.Array, false, 2)]
        public void AsyncApiSchemaGenerator_OnGeneratePrimitive_SchemaTypeAndNameIsMatch(Type type, string format, int schemaType, bool nullable, int schemaCount)
        {
            // Arrange
            AsyncApiSchemaGenerator generator = new();

            // Act
            var schema = generator.Generate(type);

            // Assert
            schema.ShouldNotBeNull();
            schema.Value.All.Count.ShouldBe(schemaCount);
            schema.Value.Root.Properties.ShouldBeEmpty();
            schema.Value.Root.Format.ShouldBe(format);
            schema.Value.Root.Type.ShouldBe((AsyncApiSchemaValueType)schemaType);
            schema.Value.Root.Nullable.ShouldBe(nullable);
        }

        [Fact]
        public void AsyncApiSchemaGenerator_OnGenerateParams_SchemaIsMatch()
        {
            // Arrange
            AsyncApiSchemaGenerator generator = new();
            var type = typeof(Foo);

            // Act
            var schema = generator.Generate(type);

            // Assert
            schema.ShouldNotBeNull();
            schema.Value.All.Count.ShouldBe(4);
            schema.Value.All.Select(x => x.Id).ShouldBe(new[] { "foo", "bar", "string", "decimal" }, ignoreOrder: true);
            schema.Value.Root.Properties.Count.ShouldBe(7);
            schema.Value.Root.Required.ShouldBe(new[] { "id", "myUri", "bar", "helloWorld", "timestamp", "fooType" }, ignoreOrder: true);

            schema.Value.Root.Properties.ShouldContainKey("id");
            var id = schema.Value.Root.Properties["id"];
            id.Type.ShouldBe(AsyncApiSchemaValueType.String);
            id.Format.ShouldBe("guid");
            id.Id.ShouldBe("guid");
            id.Nullable.ShouldBeFalse();

            schema.Value.Root.Properties.ShouldContainKey("myUri");
            var myUri = schema.Value.Root.Properties["myUri"];
            myUri.Type.ShouldBe(AsyncApiSchemaValueType.String);
            myUri.Format.ShouldBe("uri");
            myUri.Id.ShouldBe("uri");
            myUri.Nullable.ShouldBeFalse();

            schema.Value.Root.Properties.ShouldContainKey("bar");
            var bar = schema.Value.Root.Properties["bar"];
            bar.Type.ShouldBe(AsyncApiSchemaValueType.Object);
            bar.Id.ShouldBe("bar");
            bar.Format.ShouldBeNull();
            bar.Nullable.ShouldBeFalse();
            bar.Required.ShouldBe(new[] { "name" });

            bar.Properties.ShouldContainKey("name");
            var barName = bar.Properties["name"];
            barName.Type.ShouldBe(AsyncApiSchemaValueType.String);
            barName.Id.ShouldBe("string");
            barName.Format.ShouldBe("string");
            barName.Nullable.ShouldBeFalse();

            bar.Properties.ShouldContainKey("cost");
            var barCost = bar.Properties["cost"];
            barCost.Type.ShouldBe(AsyncApiSchemaValueType.Number);
            barCost.Id.ShouldBeNull();
            barCost.Format.ShouldBe("decimal");
            barCost.Nullable.ShouldBeTrue();

            schema.Value.Root.Properties.ShouldContainKey("helloWorld");
            var helloWorld = schema.Value.Root.Properties["helloWorld"];
            helloWorld.Type.ShouldBe(AsyncApiSchemaValueType.String);
            helloWorld.Id.ShouldBe("string");
            helloWorld.Format.ShouldBe("string");
            helloWorld.Nullable.ShouldBeFalse();

            schema.Value.Root.Properties.ShouldContainKey("helloWorld2");
            var helloWorld2 = schema.Value.Root.Properties["helloWorld2"];
            helloWorld2.Type.ShouldBe(AsyncApiSchemaValueType.String);
            helloWorld2.Id.ShouldBeNull();
            helloWorld2.Format.ShouldBe("string");
            helloWorld2.Nullable.ShouldBeTrue();

            schema.Value.Root.Properties.ShouldContainKey("timestamp");
            var timestamp = schema.Value.Root.Properties["timestamp"];
            timestamp.Type.ShouldBe(AsyncApiSchemaValueType.String);
            timestamp.Id.ShouldBe("dateTimeOffset");
            timestamp.Format.ShouldBe("dateTimeOffset");
            timestamp.Nullable.ShouldBeFalse();

            schema.Value.Root.Properties.ShouldContainKey("fooType");
            var fooType = schema.Value.Root.Properties["fooType"];
            fooType.Type.ShouldBe(AsyncApiSchemaValueType.String);
            fooType.Id.ShouldBe("fooType");
            fooType.Format.ShouldBe("enum");
            fooType.Nullable.ShouldBeFalse();

            fooType.EnumValues
                .SequenceEqual(Enum.GetNames<FooType>())
                .ShouldBeTrue();
        }

        [Fact]
        public void AsyncApiSchemaGenerator_OnGenerateRootReferenceType_IsNotNullableByDefault()
        {
            AsyncApiSchemaGenerator generator = new();

            var schema = generator.Generate(typeof(Foo));

            schema.ShouldNotBeNull();
            schema.Value.Root.Nullable.ShouldBeFalse();
        }

        [Fact]
        public void AsyncApiSchemaGenerator_OnGenerateEnumWithEnumMember_UsesConfiguredValues()
        {
            AsyncApiSchemaGenerator generator = new();

            var schema = generator.Generate(typeof(CommandEnvelope));

            schema.ShouldNotBeNull();
            schema.Value.Root.Properties.ShouldContainKey("command");

            var command = schema.Value.Root.Properties["command"];
            command.Type.ShouldBe(AsyncApiSchemaValueType.String);
            command.Format.ShouldBe("enum");
            command.EnumValues.ShouldBe(new[] { "on", "off" });
        }

        [Fact]
        public void AsyncApiSchemaGenerator_DoesNotKeepStaticNullabilityState()
        {
            typeof(AsyncApiSchemaGenerator)
                .GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Any(field => field.FieldType == typeof(NullabilityInfoContext))
                .ShouldBeFalse();
        }

        [Fact]
        public void AsyncApiSchemaGenerator_OnLoopGenerate_NotFailed()
        {
            // Arrange
            AsyncApiSchemaGenerator generator = new();
            var type = typeof(Loop);

            // Act
            var schema = generator.Generate(type);

            // Assert
            schema.ShouldNotBeNull();

            schema.Value.All.Count.ShouldBe(1);
            schema.Value.Root.Properties.Count.ShouldBe(2);

            schema.Value.Root.Properties.ShouldContainKey("ultraLoop");
            schema.Value.Root.Properties.ShouldContainKey("ultraLoop2");

            var loop = schema.Value.Root.Properties["ultraLoop"];
            loop.Reference.ShouldBe("#/components/schemas/loop");

            var loop2 = schema.Value.Root.Properties["ultraLoop2"];
            loop2.Nullable.ShouldBeTrue();
            loop2.AllOf.Count.ShouldBe(1);
            loop2.AllOf.Single().Reference.ShouldBe("#/components/schemas/loop");
        }

        [Fact]
        public void AsyncApiSchemaGenerator_IncludesInheritedPublicInstancePropertiesAndSkipsIndexers()
        {
            AsyncApiSchemaGenerator generator = new();

            var schema = generator.Generate(typeof(DerivedWithIndexer));

            schema.ShouldNotBeNull();
            schema.Value.Root.Properties.ShouldContainKey("baseName");
            schema.Value.Root.Properties.ShouldContainKey("derivedName");
            schema.Value.Root.Properties.ShouldNotContainKey("item");
        }

        [Fact]
        public void AsyncApiSchemaGenerator_DoesNotTreatDictionariesAsArrays()
        {
            AsyncApiSchemaGenerator generator = new();

            var schema = generator.Generate(typeof(global::System.Collections.Generic.Dictionary<string, int>));

            schema.ShouldNotBeNull();
            schema.Value.Root.Type.ShouldBe(AsyncApiSchemaValueType.Object);
            schema.Value.Root.Items.ShouldBeNull();
            schema.Value.Root.Properties.ShouldBeEmpty();
            schema.Value.Root.AdditionalProperties.ShouldNotBeNull();
            schema.Value.Root.AdditionalProperties.Type.ShouldBe(AsyncApiSchemaValueType.Integer);
            schema.Value.Root.AdditionalProperties.Format.ShouldBe("int32");
        }

        [Fact]
        public void AsyncApiSchemaGenerator_UsesAdditionalPropertiesForDictionaryProperties()
        {
            AsyncApiSchemaGenerator generator = new();

            var schema = generator.Generate(typeof(FooWithDictionary));

            schema.ShouldNotBeNull();
            var attributes = schema.Value.Root.Properties["attributes"];
            attributes.Type.ShouldBe(AsyncApiSchemaValueType.Object);
            attributes.Properties.ShouldBeEmpty();
            attributes.AdditionalProperties.ShouldNotBeNull();
            attributes.AdditionalProperties.Type.ShouldBe(AsyncApiSchemaValueType.String);
            attributes.AdditionalProperties.Format.ShouldBe("string");
        }

        [Fact]
        public void AsyncApiSchemaGenerator_ThrowsForNonStringDictionaryKeys()
        {
            AsyncApiSchemaGenerator generator = new();

            Action actual = () => { _ = generator.Generate(typeof(global::System.Collections.Generic.Dictionary<int, string>)); };

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("non-string keys");
        }

        [Fact]
        public void AsyncApiSchemaGenerator_ThrowsWhenDifferentSchemasShareTheSameId()
        {
            AsyncApiSchemaGenerator generator = new();

            Action actual = () => { _ = generator.Generate(typeof(CollisionRoot)); };

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("Conflicting schema descriptors found for id 'duplicate'");
        }

        [Fact]
        public void AsyncApiSchemaGenerator_KeepsSharedObjectSchemaNonNullableForNullableUsages()
        {
            AsyncApiSchemaGenerator generator = new();

            var schema = generator.Generate(typeof(FooWithNullableBar));

            schema.ShouldNotBeNull();
            schema.Value.All.Single(component => component.Id == "bar").Nullable.ShouldBeFalse();

            var requiredBar = schema.Value.Root.Properties["requiredBar"];
            requiredBar.Id.ShouldBe("bar");
            requiredBar.Nullable.ShouldBeFalse();

            var optionalBar = schema.Value.Root.Properties["optionalBar"];
            optionalBar.Nullable.ShouldBeTrue();
            optionalBar.AllOf.Single().Reference.ShouldBe("#/components/schemas/bar");
        }
    }

    public class Foo
    {
        public Guid Id { get; set; }
        public Uri MyUri { get; set; } = null!;
        public Bar Bar { get; set; } = null!;
        public string HelloWorld { get; set; } = null!;
        public string? HelloWorld2 { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public FooType FooType { get; set; }
    }

    public enum FooType { Foo, Bar }

    public class CommandEnvelope
    {
        public CommandType Command { get; set; }
    }

    public enum CommandType
    {
        [EnumMember(Value = "on")]
        On,
        [EnumMember(Value = "off")]
        Off
    }

    public class Bar
    {
        public string Name { get; set; } = null!;
        public decimal? Cost { get; set; }
    }

    public class Loop
    {
        public Loop UltraLoop { get; set; } = null!;
        public Loop? UltraLoop2 { get; set; }
    }

    public class FooWithNullableBar
    {
        public Bar RequiredBar { get; set; } = null!;
        public Bar? OptionalBar { get; set; }
    }

    public class FooWithDictionary
    {
        public global::System.Collections.Generic.Dictionary<string, string> Attributes { get; set; } = new();
    }

    public class BaseWithProperty
    {
        public string BaseName { get; set; } = string.Empty;
    }

    public class DerivedWithIndexer : BaseWithProperty
    {
        public string DerivedName { get; set; } = string.Empty;

        public string this[int index] => index.ToString();
    }

    public class CollisionRoot
    {
        public global::Saunter.Tests.SharedKernel.Collisions.One.Duplicate DuplicateOne { get; set; } = null!;
        public global::Saunter.Tests.SharedKernel.Collisions.Two.Duplicate DuplicateTwo { get; set; } = null!;
    }
}

namespace Saunter.Tests.SharedKernel.Collisions.One
{
    public class Duplicate
    {
        public string Value { get; set; } = string.Empty;
    }
}

namespace Saunter.Tests.SharedKernel.Collisions.Two
{
    public class Duplicate
    {
        public int Value { get; set; }
    }
}
