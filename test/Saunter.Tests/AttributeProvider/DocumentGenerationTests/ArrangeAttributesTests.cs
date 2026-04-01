using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Saunter.AttributeProvider;
using Saunter.Options;

namespace Saunter.Tests.AttributeProvider.DocumentGenerationTests
{
    internal static class ArrangeAttributesTests
    {
        private sealed class FakeAsyncApiOptions : AsyncApiOptions
        {
            private readonly TypeInfo[] _types;

            public FakeAsyncApiOptions(Type[] types)
            {
                _types = types.Select(t => t.GetTypeInfo()).ToArray();
            }

            internal override IReadOnlyCollection<TypeInfo> AsyncApiSchemaTypes => _types;
        }

        public static void Arrange(out AsyncApiOptions options, out AttributeDocumentProvider documentProvider, params Type[] targetTypes)
        {
            options = new FakeAsyncApiOptions(targetTypes);

            var services = new ServiceCollection();
            services.AddFakeLogging();
            services.AddAsyncApiSchemaGeneration();

            var serviceProvider = services.BuildServiceProvider();
            documentProvider = (AttributeDocumentProvider)serviceProvider.GetRequiredService<IAsyncApiDocumentProvider>();
        }
    }
}
