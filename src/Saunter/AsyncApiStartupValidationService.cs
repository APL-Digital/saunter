using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saunter.Options;

namespace Saunter
{
    /// <summary>
    /// Generates every registered AsyncAPI document once at application startup so misconfiguration
    /// surfaces as a startup failure instead of a 500 on the first request to the document endpoint.
    /// Controlled by <see cref="AsyncApiOptions.ValidateOnStartup"/>: on by default in the
    /// Development environment, off elsewhere unless explicitly enabled.
    /// </summary>
    internal sealed class AsyncApiStartupValidationService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<AsyncApiStartupValidationService> _logger;
        private readonly AsyncApiOptions _options;

        public AsyncApiStartupValidationService(
            IServiceProvider serviceProvider,
            IHostEnvironment environment,
            IOptions<AsyncApiOptions> options,
            ILogger<AsyncApiStartupValidationService> logger)
        {
            _serviceProvider = serviceProvider;
            _environment = environment;
            _options = options.Value;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var validate = _options.ValidateOnStartup ?? _environment.IsDevelopment();
            if (!validate)
            {
                return Task.CompletedTask;
            }

            using var scope = _serviceProvider.CreateScope();
            var provider = scope.ServiceProvider.GetRequiredService<IAsyncApiDocumentProvider>();

            foreach (var documentName in GetDocumentNames())
            {
                provider.GetDocument(documentName, _options);
                _logger.LogDebug("AsyncAPI document '{DocumentName}' validated at startup.", documentName ?? "<default>");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private IEnumerable<string?> GetDocumentNames()
        {
            if (_options.Documents.Count > 0 || _options.NamedApis.Count > 0)
            {
                return _options.Documents.Keys
                    .Concat(_options.NamedApis.Keys)
                    .Distinct()
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .Cast<string?>();
            }

            return new string?[] { null };
        }
    }
}
