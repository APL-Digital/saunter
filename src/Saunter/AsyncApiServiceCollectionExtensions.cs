using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Saunter.AttributeProvider;
using Saunter.Options;
using Saunter.SharedKernel;
using Saunter.SharedKernel.Interfaces;

namespace Saunter
{
    /// <summary>
    /// Extension methods for registering and configuring Saunter's AsyncAPI document generation
    /// on an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class AsyncApiServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the services required for AsyncAPI document generation (document provider, writer,
        /// schema generator, etc.) and optionally configures <see cref="AsyncApiOptions"/>.
        /// </summary>
        /// <param name="services">The service collection to add the services to.</param>
        /// <param name="setupAction">An optional action to configure <see cref="AsyncApiOptions"/>.</param>
        /// <returns>The same service collection, for chaining.</returns>
        public static IServiceCollection AddAsyncApiSchemaGeneration(this IServiceCollection services, Action<AsyncApiOptions>? setupAction = null)
        {
            services.AddOptions();

            services.TryAddSingleton<IAsyncApiDocumentWriter, AsyncApiDocumentWriter>();
            services.TryAddSingleton<IAsyncApiDocumentCloner, AsyncApiDocumentSerializeCloner>();
            services.TryAddSingleton<IAsyncApiDocumentMapper, AsyncApiDocumentMapper>();
            services.TryAddSingleton<IAsyncApiSchemaGenerator, AsyncApiSchemaGenerator>();
            services.TryAddSingleton<IAsyncApiSchemaMapper, AsyncApiSchemaMapper>();
            services.TryAddSingleton<IAsyncApiChannelUnion, AsyncApiChannelUnion>();
            services.TryAddSingleton<IAsyncApiDescriptorMapper, AsyncApiDescriptorMapper>();
            services.TryAddSingleton<IAttributeMessageResolver, AttributeMessageResolver>();
            services.TryAddSingleton<IAttributeChannelBuilder, AttributeChannelBuilder>();
            services.TryAddSingleton<IAttributeOperationBuilder, AttributeOperationBuilder>();
            services.TryAddSingleton<IAsyncApiDocumentValidator, AsyncApiDocumentValidator>();
            services.TryAddTransient<IAsyncApiDocumentProvider, AttributeDocumentProvider>();

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }

            return services;
        }

        /// <summary>
        /// Configures a named document prototype in <see cref="AsyncApiOptions.NamedApis"/>, and switches
        /// the shared middleware routes to <c>{document}</c>-templated routes if they are not already.
        /// Types annotated with <c>[AsyncApi("name")]</c> matching <paramref name="documentName"/> are
        /// included in the document. For per-document routes, marker types and type filtering, use
        /// <see cref="ConfigureAsyncApiDocument"/> instead.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="documentName">The document name; created in <see cref="AsyncApiOptions.NamedApis"/> if it does not exist.</param>
        /// <param name="setupAction">An action to configure the document prototype.</param>
        /// <returns>The same service collection, for chaining.</returns>
        public static IServiceCollection ConfigureNamedAsyncApi(this IServiceCollection services, string documentName, Action<AsyncApiDocumentDescriptor> setupAction)
        {
            services.Configure<AsyncApiOptions>(options =>
            {
                if (options.Middleware.Route == null
                    || !options.Middleware.Route.Contains("{document}", StringComparison.OrdinalIgnoreCase)
                    || options.Middleware.UiBaseRoute == null
                    || !options.Middleware.UiBaseRoute.Contains("{document}", StringComparison.OrdinalIgnoreCase))
                {
                    options.Middleware.Route = "/asyncapi/{document}/asyncapi.json";
                    options.Middleware.UiBaseRoute = "/asyncapi/{document}/ui/";
                }

                var document = options.NamedApis.GetOrAdd(documentName, _ => new AsyncApiDocumentDescriptor());
                setupAction(document);
            });

            return services;
        }

        /// <summary>
        /// Configures a full document registration in <see cref="AsyncApiOptions.Documents"/>, including
        /// its own routes (defaulting to <c>/asyncapi/{documentName}/asyncapi.json</c> and
        /// <c>/asyncapi/{documentName}/ui</c>), marker types and type filter. Registrations configured
        /// here take precedence over <see cref="AsyncApiOptions.NamedApis"/> entries with the same name.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="documentName">The document name; created in <see cref="AsyncApiOptions.Documents"/> if it does not exist.</param>
        /// <param name="setupAction">An action to configure the document registration.</param>
        /// <returns>The same service collection, for chaining.</returns>
        public static IServiceCollection ConfigureAsyncApiDocument(this IServiceCollection services, string documentName, Action<AsyncApiDocumentRegistration> setupAction)
        {
            services.Configure<AsyncApiOptions>(options =>
            {
                var document = options.Documents.GetOrAdd(documentName, CreateDocumentRegistration);
                setupAction(document);
            });

            return services;
        }

        private static AsyncApiDocumentRegistration CreateDocumentRegistration(string documentName)
        {
            var document = new AsyncApiDocumentRegistration(documentName);
            document.Middleware.Route = $"/asyncapi/{documentName}/asyncapi.json";
            document.Middleware.UiBaseRoute = $"/asyncapi/{documentName}/ui";
            return document;
        }
    }
}
