using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Saunter.AttributeProvider;
using Saunter.Options;
using Saunter.SharedKernel;
using Saunter.SharedKernel.Interfaces;

namespace Saunter
{
    public static class AsyncApiServiceCollectionExtensions
    {
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

        public static IServiceCollection ConfigureNamedAsyncApi(this IServiceCollection services, string documentName, Action<AsyncApiDocumentDescriptor> setupAction)
        {
            services.Configure<AsyncApiOptions>(options =>
            {
                if (options.Middleware.Route == null
                    || !options.Middleware.Route.ToLower().Contains("{document}")
                    || options.Middleware.UiBaseRoute == null
                    || !options.Middleware.UiBaseRoute.ToLower().Contains("{document}"))
                {
                    options.Middleware.Route = "/asyncapi/{document}/asyncapi.json";
                    options.Middleware.UiBaseRoute = "/asyncapi/{document}/ui/";
                }

                var document = options.NamedApis.GetOrAdd(documentName, _ => new AsyncApiDocumentDescriptor());
                setupAction(document);
            });

            return services;
        }
    }
}
