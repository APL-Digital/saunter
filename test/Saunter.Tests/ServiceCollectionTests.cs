using System;
using System.Collections.Generic;
using ByteBard.AsyncAPI.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
            var options = sp.GetRequiredService<IOptions<AsyncApiOptions>>().Value;
            var document = provider.GetDocument(null, options);

            document.ShouldNotBeNull();
            document.Asyncapi.ShouldStartWith("3");
            document.Info.Title.ShouldBe("Example API");
        }

        [Fact]
        public void ConfigureNamedAsyncApi_PreservesCustomRouteTokensRegardlessOfTokenCasing()
        {
            var services = new ServiceCollection();

            services.AddAsyncApiSchemaGeneration(options =>
            {
                options.Middleware.Route = "/asyncapi/{Document}/asyncapi.json";
                options.Middleware.UiBaseRoute = "/asyncapi/{DOCUMENT}/ui/";
            });
            services.ConfigureNamedAsyncApi("orders", document =>
            {
                document.Info = new AsyncApiInfoDescriptor
                {
                    Title = "Orders",
                    Version = "1.0.0",
                };
            });

            using var sp = services.BuildServiceProvider();
            var options = sp.GetRequiredService<IOptions<AsyncApiOptions>>().Value;

            options.Middleware.Route.ShouldBe("/asyncapi/{Document}/asyncapi.json");
            options.Middleware.UiBaseRoute.ShouldBe("/asyncapi/{DOCUMENT}/ui/");
        }

        [Fact]
        public void GetDocument_UsesNamedDocumentConfiguredByDictionaryKey()
        {
            var services = new ServiceCollection();

            services.AddFakeLogging();
            services.AddAsyncApiSchemaGeneration(options =>
            {
                options.AsyncApi = new AsyncApiDocumentDescriptor
                {
                    Info = new AsyncApiInfoDescriptor
                    {
                        Title = "Default",
                        Version = "1.0.0",
                    },
                };
            });
            services.ConfigureNamedAsyncApi("orders", document =>
            {
                document.Info = new AsyncApiInfoDescriptor
                {
                    Title = "Orders",
                    Version = "2.0.0",
                };
            });

            using var sp = services.BuildServiceProvider();
            var provider = sp.GetRequiredService<IAsyncApiDocumentProvider>();
            var options = sp.GetRequiredService<IOptions<AsyncApiOptions>>().Value;

            var document = provider.GetDocument("orders", options);

            document.Info.Title.ShouldBe("Orders");
            document.Info.Version.ShouldBe("2.0.0");
        }

        [Fact]
        public void GetDocument_CreatesConfiguredFilterWithoutExplicitFilterRegistration()
        {
            var services = new ServiceCollection();

            services.AddFakeLogging();
            services.AddAsyncApiSchemaGeneration(options =>
            {
                options.AsyncApi = new AsyncApiDocumentDescriptor
                {
                    Info = new AsyncApiInfoDescriptor
                    {
                        Title = "Filters",
                        Version = "1.0.0",
                    },
                };
                options.AddDocumentFilter<StandaloneDocumentFilter>();
            });

            using var sp = services.BuildServiceProvider();
            var provider = sp.GetRequiredService<IAsyncApiDocumentProvider>();
            var options = sp.GetRequiredService<IOptions<AsyncApiOptions>>().Value;

            var document = provider.GetDocument(null, options);

            document.Info.Description.ShouldBe("created without explicit DI registration");
        }

        [Fact]
        public void GetDocument_UsesDiDependenciesWhenCreatingConfiguredFilter()
        {
            var services = new ServiceCollection();

            services.AddFakeLogging();
            services.AddSingleton(new FilterDependency("dependency injected"));
            services.AddAsyncApiSchemaGeneration(options =>
            {
                options.AsyncApi = new AsyncApiDocumentDescriptor
                {
                    Info = new AsyncApiInfoDescriptor
                    {
                        Title = "Filters",
                        Version = "1.0.0",
                    },
                };
                options.AddDocumentFilter<DependentDocumentFilter>();
            });

            using var sp = services.BuildServiceProvider();
            var provider = sp.GetRequiredService<IAsyncApiDocumentProvider>();
            var options = sp.GetRequiredService<IOptions<AsyncApiOptions>>().Value;

            var document = provider.GetDocument(null, options);

            document.Info.Description.ShouldBe("dependency injected");
        }

        private sealed record FilterDependency(string Description);

        private sealed class StandaloneDocumentFilter : Options.Filters.IDocumentFilter
        {
            public void Apply(AsyncApiDocumentDescriptor document, global::DocumentFilterContext context)
            {
                document.Info.Description = "created without explicit DI registration";
            }
        }

        private sealed class DependentDocumentFilter : Options.Filters.IDocumentFilter
        {
            private readonly FilterDependency _dependency;

            public DependentDocumentFilter(FilterDependency dependency)
            {
                _dependency = dependency;
            }

            public void Apply(AsyncApiDocumentDescriptor document, global::DocumentFilterContext context)
            {
                document.Info.Description = _dependency.Description;
            }
        }
    }
}
