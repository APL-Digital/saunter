using System;
using Saunter.SharedKernel;
using Saunter.SharedKernel.Descriptors;
using Shouldly;
using Xunit;

namespace Saunter.Tests.SharedKernel
{
    public class DocumentWriterTests
    {
        [Fact]
        public void WriteJson_UsesJsonSchemaNullabilityForAsyncApi3()
        {
            var writer = new AsyncApiDocumentWriter(new AsyncApiDocumentMapper(new global::Saunter.AttributeProvider.AsyncApiDescriptorMapper(new AsyncApiSchemaMapper())));
            var document = new AsyncApiDocumentDescriptor
            {
                Asyncapi = "3.1.0",
                Info = new AsyncApiInfoDescriptor
                {
                    Title = "test",
                    Version = "1.0.0"
                },
                Components = new AsyncApiComponentsDescriptor
                {
                    Schemas =
                    {
                        ["payload"] = new AsyncApiSchemaDescriptor
                        {
                            Id = "payload",
                            Type = AsyncApiSchemaValueType.Object,
                            Nullable = true
                        }
                    }
                }
            };

            var json = writer.WriteJson(document);

            json.ShouldNotContain("\"nullable\"");
            json.ShouldContain("\"oneOf\"");
            json.ShouldContain("\"type\": \"null\"");
        }

        [Fact]
        public void WriteJson_ThrowsForUnsupportedAsyncApiVersion()
        {
            var writer = new AsyncApiDocumentWriter(new AsyncApiDocumentMapper(new global::Saunter.AttributeProvider.AsyncApiDescriptorMapper(new AsyncApiSchemaMapper())));
            var document = new AsyncApiDocumentDescriptor
            {
                Asyncapi = "4.0.0",
                Info = new AsyncApiInfoDescriptor
                {
                    Title = "test",
                    Version = "1.0.0"
                }
            };

            var actual = () => writer.WriteJson(document);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("Unsupported AsyncAPI version");
        }
    }
}
