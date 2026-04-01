using System.Linq;
using ByteBard.AsyncAPI.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Saunter.AttributeProvider.Attributes;
using Saunter.AttributeProvider.Descriptors;
using Saunter.Options;
using Saunter.Options.Filters;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider
{
    public class OperationTraitsTests
    {
        private const string DocumentName = "operation-traits";

        [Fact]
        public void Example_OperationTraits()
        {
            var services = new ServiceCollection();

            services.AddFakeLogging();
            services.AddTransient<TestOperationTraitsFilter>();
            services.AddAsyncApiSchemaGeneration(o =>
            {
                o.AsyncApi = new AsyncApiDocumentDescriptor
                {
                    Asyncapi = "3.0.0",
                    Info = new AsyncApiInfoDescriptor
                    {
                        Title = GetType().FullName,
                        Version = "1.0.0"
                    },
                    Components = new AsyncApiComponentsDescriptor
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
            var document = documentProvider.GetDocument(DocumentName, options);

            document.Components.OperationTraits.ShouldContainKey("exampleTrait");
            document.Operations.ShouldContainKey("AnnotatedOperation");
            document.Operations["AnnotatedOperation"].TraitReferences.ShouldContain("exampleTrait");
        }

        private class TestOperationTraitsFilter : IOperationFilter
        {
            public void Apply(AsyncApiOperationDescriptor operation, OperationFilterContext context)
            {
                operation.TraitReferences.Add("exampleTrait");
            }
        }

        [AsyncApi("operation-traits")]
        [Channel("trait.example", "trait.example")]
        [SendOperation(OperationId = "AnnotatedOperation")]
        private class AnnotatedOperation
        {
            [Message(typeof(string))]
            public void Publish() { }
        }
    }
}
