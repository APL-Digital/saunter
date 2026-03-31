using Saunter.SharedKernel;
using Saunter.SharedKernel.Descriptors;
using Shouldly;
using Xunit;

namespace Saunter.Tests.SharedKernel
{
    public class DocumentWriterTests
    {
        [Fact]
        public void WriteJson_PreservesNullableKeyword()
        {
            var writer = new AsyncApiDocumentWriter();
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

            json.ShouldContain("\"nullable\"");
            json.ShouldNotContain("\"oneOf\"");
            json.ShouldNotContain("\"type\": \"null\"");
        }
    }
}
