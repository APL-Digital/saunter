using System;
using System.Collections.Generic;
using System.IO;
using ByteBard.AsyncAPI.Models;
using Microsoft.Extensions.DependencyInjection;
using Saunter.Options;
using Shouldly;
using Xunit;

namespace Saunter.Tests
{
    public class ServiceCollectionTests
    {
        [Fact]
        public void TestAddAsyncApiSchemaGeneration()
        {
            var services = new ServiceCollection();

            services.AddFakeLogging();
            services.AddAsyncApiSchemaGeneration(options =>
            {
                options.AsyncApi = new AsyncApiDocumentDescriptor
                {
                    Id = "urn:com:example:example-events",
                    Asyncapi = "3.0.0",
                    Info = new AsyncApiInfoDescriptor
                    {
                        Title = "Example API",
                        Version = "2019.01.12345",
                        Description = "An example API with events",
                        Contact = new AsyncApiContactDescriptor
                        {
                            Email = "michael@mwild.me",
                            Name = "Michael Wildman",
                            Url = new("https://mwild.me/"),
                        },
                        License = new AsyncApiLicenseDescriptor
                        {
                            Name = "MIT",
                        },
                        TermsOfService = new("https://mwild.me/tos"),
                    },
                    Servers =
                    {
                        ["development"] = new AsyncApiServerDescriptor
                        {
                            Protocol = "amqp",
                            Host = "rabbitmq.dev.mwild.me",
                            Security = new List<AsyncApiSecurityScheme>()
                        }
                    },
                    Components = new AsyncApiComponentsDescriptor
                    {
                        SecuritySchemes =
                        {
                            ["user-password"] = new AsyncApiSecurityScheme { Type = SecuritySchemeType.Http }
                        }
                    }
                };
            });

            var sp = services.BuildServiceProvider();

            var provider = sp.GetRequiredService<IAsyncApiDocumentProvider>();
            var document = provider.GetDocument(null, new AsyncApiOptions());

            document.ShouldNotBeNull();
            document.Asyncapi.ShouldStartWith("3");
        }

        [Fact]
        public void ConfigureNamedAsyncApi_UsesOrdinalIgnoreCaseDocumentTokenMatching()
        {
            var sourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/Saunter/AsyncApiServiceCollectionExtensions.cs"));
            var source = File.ReadAllText(sourcePath);

            source.ShouldContain("Contains(\"{document}\", StringComparison.OrdinalIgnoreCase)");
            source.ShouldNotContain(".ToLower().Contains(\"{document}\")");
        }
    }
}
