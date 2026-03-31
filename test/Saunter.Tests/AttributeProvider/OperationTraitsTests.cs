using ByteBard.AsyncAPI.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Saunter.Options;
using Saunter.Options.Filters;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider
{
    public class OperationTraitsTests
    {
        [Fact]
        public void Example_OperationTraits()
        {
            var services = new ServiceCollection();

            services.AddFakeLogging();
            services.AddAsyncApiSchemaGeneration(o =>
            {
                o.AsyncApi = new AsyncApiDocument
                {
                    Asyncapi = "3.0.0",
                    Info = new AsyncApiInfo
                    {
                        Title = GetType().FullName,
                        Version = "1.0.0"
                    },
                    Components = new()
                    {
                        OperationTraits =
                        {
                            ["exampleTrait"] = new AsyncApiOperationTrait { Description = "This is an example trait" }
                        }
                    }
                };

                o.AssemblyMarkerTypes = new[] { typeof(AnnotatedOperation) };
                o.AddOperationFilter<TestOperationTraitsFilter>();
            });

            using var serviceprovider = services.BuildServiceProvider();

            var documentProvider = serviceprovider.GetRequiredService<IAsyncApiDocumentProvider>();
            var options = serviceprovider.GetRequiredService<IOptions<AsyncApiOptions>>().Value;
            var document = documentProvider.GetDocument(null, options);

            document.Components.OperationTraits.ShouldContainKey("exampleTrait");
            document.Operations.ShouldContainKey("AnnotatedOperation");
        }

        private class TestOperationTraitsFilter : IOperationFilter
        {
            public void Apply(AsyncApiOperation operation, OperationFilterContext context)
            {
                operation.Traits.Add(new AsyncApiOperationTraitReference("#/components/operationTraits/exampleTrait"));
            }
        }

        [AsyncApi]
        [Channel("trait.example", "trait.example")]
        [SendOperation(OperationId = "AnnotatedOperation")]
        private class AnnotatedOperation
        {
            [Message(typeof(string))]
            public void Publish() { }
        }
    }
}
