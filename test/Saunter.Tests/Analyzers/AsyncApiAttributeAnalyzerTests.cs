using System;
#nullable enable
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Saunter.Analyzers;
using Saunter.AttributeProvider.Attributes;
using Shouldly;
using Xunit;

namespace Saunter.Tests.Analyzers
{
    public class AsyncApiAttributeAnalyzerTests
    {
        [Fact]
        public async Task AnalyzeAsync_DetectsDuplicateOperationIds()
        {
            const string source = """
using Saunter.AttributeProvider.Attributes;

[AsyncApi]
[Channel("orders", "orders")]
public class OrdersApi
{
    [SendOperation(OperationId = "publish")]
    public void Publish() { }
}

[AsyncApi]
[Channel("payments", "payments")]
public class PaymentsApi
{
    [SendOperation(OperationId = "publish")]
    public void Publish() { }
}
""";

            var diagnostics = await AnalyzeAsync(source);

            diagnostics.ShouldContain(diagnostic => diagnostic.Id == AsyncApiAttributeAnalyzer.DuplicateOperationIdDiagnosticId);
        }

        [Fact]
        public async Task AnalyzeAsync_DetectsInvalidExternalDocs()
        {
            const string source = """
using Saunter.AttributeProvider.Attributes;

[AsyncApi]
public class OrdersApi
{
    [Channel("orders", "orders")]
    [SendOperation]
    [Message(typeof(string), ExternalDocs = "not a url")]
    public void Publish() { }
}
""";

            var diagnostics = await AnalyzeAsync(source);

            diagnostics.ShouldContain(diagnostic => diagnostic.Id == AsyncApiAttributeAnalyzer.InvalidExternalDocsDiagnosticId);
        }

        [Fact]
        public async Task AnalyzeAsync_DetectsChannelParameterMismatch()
        {
            const string source = """
using Saunter.AttributeProvider.Attributes;

[AsyncApi]
public class OrdersApi
{
    [Channel("orders", "orders.created")]
    [ChannelParameter("tenantId")]
    [SendOperation]
    public void Publish() { }
}
""";

            var diagnostics = await AnalyzeAsync(source);

            diagnostics.ShouldContain(diagnostic => diagnostic.Id == AsyncApiAttributeAnalyzer.ChannelParameterMismatchDiagnosticId);
        }

        [Fact]
        public async Task AnalyzeAsync_DetectsInvalidReferenceNames()
        {
            const string source = """
using Saunter.AttributeProvider.Attributes;

[AsyncApi]
public class OrdersApi
{
    [Channel("orders", "orders.created", Servers = new[] { "bad ref" })]
    [SendOperation(BindingsRef = "bad ref")]
    [Message(typeof(string), CorrelationId = "bad ref")]
    public void Publish() { }
}
""";

            var diagnostics = await AnalyzeAsync(source);

            diagnostics.Count(diagnostic => diagnostic.Id == AsyncApiAttributeAnalyzer.InvalidReferenceNameDiagnosticId).ShouldBeGreaterThanOrEqualTo(3);
        }

        [Fact]
        public async Task AnalyzeAsync_DetectsOrphanedMessageAttributes()
        {
            const string source = """
using Saunter.AttributeProvider.Attributes;

[AsyncApi]
public class OrdersApi
{
    [Message(typeof(string))]
    public void Publish() { }
}
""";

            var diagnostics = await AnalyzeAsync(source);

            diagnostics.ShouldContain(diagnostic => diagnostic.Id == AsyncApiAttributeAnalyzer.OrphanedAnnotationDiagnosticId);
        }

        private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
        {
            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
            var runtimeReferences = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                .ShouldNotBeNull()
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path));
            var metadataReferences = runtimeReferences
                .Concat(new[]
                {
                    MetadataReference.CreateFromFile(typeof(AsyncApiAttribute).Assembly.Location),
                })
                .GroupBy(reference => reference.Display, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();
            var compilation = CSharpCompilation.Create(
                "AnalyzerTests",
                new[] { CSharpSyntaxTree.ParseText(source, parseOptions) },
                metadataReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

            var analyzer = new AsyncApiAttributeAnalyzer();
            var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer)).GetAnalyzerDiagnosticsAsync();
            return diagnostics;
        }
    }
}
