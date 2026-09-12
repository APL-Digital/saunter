#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Saunter;
using Saunter.SharedKernel;
using Shouldly;
using Xunit;

namespace Saunter.Tests.SharedKernel
{
    public class SchemaAnnotationTests
    {
        [Fact]
        public void Generated_document_preserves_property_bounds_and_policy_descriptions()
        {
            using var document = Generate<BoundedRequest>();
            var properties = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("payload").GetProperty("properties");
            properties.GetProperty("items").GetProperty("maxItems").GetInt32().ShouldBe(100);
            properties.GetProperty("items").GetProperty("minItems").GetInt32().ShouldBe(1);
            properties.GetProperty("items").GetProperty("description").GetString().ShouldBe("Whole request is at most 6 MiB.");
            properties.GetProperty("pageSize").GetProperty("minimum").GetDouble().ShouldBe(1);
            properties.GetProperty("pageSize").GetProperty("maximum").GetDouble().ShouldBe(100);
            properties.GetProperty("name").GetProperty("maxLength").GetInt32().ShouldBe(24);
            properties.GetProperty("name").GetProperty("minLength").GetInt32().ShouldBe(2);
        }

        [Fact]
        public void Nullable_and_repeated_properties_keep_their_own_constraints()
        {
            using var document = Generate<RepeatedBounds>();
            var properties = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("payload").GetProperty("properties");
            properties.GetProperty("small").GetProperty("maxItems").GetInt32().ShouldBe(2);
            properties.GetProperty("large").GetProperty("maxItems").GetInt32().ShouldBe(100);
            properties.GetProperty("optional").GetProperty("oneOf")[0].GetProperty("maxItems").GetInt32().ShouldBe(4);
            properties.GetProperty("unbounded").TryGetProperty("maxItems", out _).ShouldBeFalse();
        }

        [Fact]
        public void Byte_array_length_limits_describe_the_base64_wire_value()
        {
            using var document = Generate<BinaryRequest>();
            var content = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("payload").GetProperty("properties").GetProperty("content");
            content.GetProperty("maxLength").GetInt32().ShouldBe(8);
            content.TryGetProperty("maxItems", out _).ShouldBeFalse();
        }

        [Fact]
        public void Partner_export_example_documents_its_identifier_bound()
        {
            using var document = Generate<MassTransitUseCases.Contracts.PartnerExportRequested>();
            var partner = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("payload").GetProperty("properties").GetProperty("partnerId");
            partner = partner.GetProperty("oneOf")[0];
            partner.GetProperty("maxLength").GetInt32().ShouldBe(64);
            partner.GetProperty("minLength").GetInt32().ShouldBe(1);
        }

        [Fact]
        public void Partner_export_sender_rejects_an_oversized_identifier()
        {
            var sender = new MassTransitUseCases.Producers.PartnerExportPublisher(NSubstitute.Substitute.For<MassTransit.IPublishEndpoint>());
            Should.Throw<ValidationException>(() => sender.Publish(new MassTransitUseCases.Contracts.PartnerExportRequested
            {
                PartnerId = new string('x', 65)
            }));
        }

        [Theory]
        [InlineData(typeof(PreciseIntegerRange))]
        [InlineData(typeof(PreciseDecimalRange))]
        [InlineData(typeof(ExclusiveRange))]
        public void Unsupported_numeric_bounds_fail_instead_of_changing_the_schema_boundary(Type payload)
        {
            var exception = Should.Throw<InvalidOperationException>(() => new AsyncApiSchemaGenerator().Generate(payload));
            exception.Message.ShouldContain("inclusive int or double bounds");
        }

        public class PreciseIntegerRange
        {
            [Range(typeof(long), "9007199254740993", "9007199254740995")]
            public long Value { get; set; }
        }

        public class PreciseDecimalRange
        {
            [Range(typeof(decimal), "0.1234567890123456789012345678", "1")]
            public decimal Value { get; set; }
        }

        public class ExclusiveRange
        {
            [Range(1, 100, MinimumIsExclusive = true)]
            public int Value { get; set; }
        }

        private static JsonDocument Generate<T>()
        {
            var generated = new AsyncApiSchemaGenerator().Generate(typeof(T))!.Value;
            var document = new AsyncApiDocumentDescriptor
            {
                Asyncapi = "3.0.0",
                Info = new AsyncApiInfoDescriptor { Title = "constraints", Version = "1.0.0" },
                Components = new AsyncApiComponentsDescriptor { Schemas = { ["payload"] = generated.Root } }
            };
            foreach (var schema in generated.All)
            {
                document.Components.Schemas[schema.Id!] = schema;
            }
            var writer = new AsyncApiDocumentWriter(new AsyncApiDocumentMapper(new Saunter.AttributeProvider.AsyncApiDescriptorMapper(new AsyncApiSchemaMapper())));
            return JsonDocument.Parse(writer.WriteJson(document));
        }

        public class BoundedRequest
        {
            [MinLength(1), MaxLength(100), Description("Whole request is at most 6 MiB.")]
            public IReadOnlyList<string> Items { get; set; } = [];
            [Range(1, 100)]
            public int PageSize { get; set; }
            [StringLength(24, MinimumLength = 2)]
            public string Name { get; set; } = "";
        }

        public class RepeatedBounds
        {
            [MaxLength(2)]
            public IReadOnlyList<string> Small { get; set; } = [];
            [MaxLength(100)]
            public IReadOnlyList<string> Large { get; set; } = [];
            [MaxLength(4)]
            public IReadOnlyList<string>? Optional { get; set; }
            public IReadOnlyList<string> Unbounded { get; set; } = [];
        }

        public class BinaryRequest
        {
            [MaxLength(5)]
            public byte[] Content { get; set; } = [];
        }
    }
}
