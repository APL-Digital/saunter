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
        public async Task AnalyzeAsync_ReportsDuplicateOperationIdDeterministicallyOnLaterOccurrences()
        {
            const string source = """
using Saunter.AttributeProvider.Attributes;

[AsyncApi]
[Channel("orders", "orders")]
public class OrdersApi
{
    [SendOperation(OperationId = "publish")]
    public void PublishFirst() { }

    [SendOperation(OperationId = "publish")]
    public void PublishSecond() { }

    [SendOperation(OperationId = "publish")]
    public void PublishThird() { }
}
""";

            var duplicates = (await AnalyzeAsync(source))
                .Where(diagnostic => diagnostic.Id == AsyncApiAttributeAnalyzer.DuplicateOperationIdDiagnosticId)
                .ToArray();

            // Three occurrences => the first is the canonical definition, the two later
            // ones are flagged, and the reported spans are ordered by source position.
            duplicates.Length.ShouldBe(2);
            var starts = duplicates.Select(diagnostic => diagnostic.Location.SourceSpan.Start).ToArray();
            starts.ShouldBe(starts.OrderBy(start => start).ToArray());
            starts[0].ShouldBeGreaterThan(source.IndexOf("PublishFirst", StringComparison.Ordinal));
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
        public async Task AnalyzeAsync_DetectsInvalidChannelParameterNames()
        {
            const string source = """
using Saunter.AttributeProvider.Attributes;

[AsyncApi]
public class OrdersApi
{
    [Channel("orders", "orders/{tenantId}")]
    [ChannelParameter("tenant.id")]
    [SendOperation]
    public void Publish() { }
}
""";

            var diagnostics = await AnalyzeAsync(source);

            diagnostics.ShouldContain(diagnostic => diagnostic.Id == AsyncApiAttributeAnalyzer.InvalidChannelParameterNameDiagnosticId);
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

        [Fact]
        public async Task AnalyzeAsync_DetectsMutuallyExclusiveReplyAddressSettings()
        {
            const string source = """
using Saunter.AttributeProvider.Attributes;

[AsyncApi]
public class OrdersApi
{
    [Channel("orders", "orders.created")]
    [SendOperation(Reply = "ordersReply", ReplyChannelAddress = "orders.reply", ReplyAddressLocation = "$message.header#/replyTo")]
    public void Publish() { }
}
""";

            var diagnostics = await AnalyzeAsync(source);

            diagnostics.ShouldContain(diagnostic => diagnostic.Id == AsyncApiAttributeAnalyzer.InvalidReplyConfigurationDiagnosticId);
        }

        [Fact]
        public async Task AnalyzeAsync_DetectsReplyMetadataWithoutReplyChannelId()
        {
            const string source = """
using Saunter.AttributeProvider.Attributes;

[AsyncApi]
public class OrdersApi
{
    [Channel("orders", "orders.created")]
    [SendOperation(ReplyChannelAddress = "orders.reply")]
    public void PublishWithAddress() { }

    [Channel("payments", "payments.created")]
    [SendOperation(ReplyMessagePayloadType = typeof(string))]
    public void PublishWithPayload() { }
}
""";

            var diagnostics = await AnalyzeAsync(source);

            diagnostics.Count(diagnostic => diagnostic.Id == AsyncApiAttributeAnalyzer.InvalidReplyConfigurationDiagnosticId).ShouldBe(2);
        }

        [Fact]
        public async Task AnalyzeAsync_AllowsValidReplyConfiguration()
        {
            const string source = """
using Saunter.AttributeProvider.Attributes;

[AsyncApi]
public class OrdersApi
{
    [Channel("orders", "orders.created")]
    [SendOperation(Reply = "ordersReply", ReplyChannelAddress = "orders.reply", ReplyMessagePayloadType = typeof(string))]
    public void Publish() { }
}
""";

            var diagnostics = await AnalyzeAsync(source);

            diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == AsyncApiAttributeAnalyzer.InvalidReplyConfigurationDiagnosticId);
        }

        [Fact]
        public async Task AnalyzeAsync_DetectsReplyMessageWithoutReplyChannelId()
        {
            const string source = """
using Saunter.AttributeProvider.Attributes;

[AsyncApi]
public class OrdersApi
{
    [Channel("orders", "orders.created")]
    [SendOperation]
    [ReplyMessage(typeof(string))]
    public void Publish() { }
}
""";

            var diagnostics = await AnalyzeAsync(source);

            diagnostics.ShouldContain(diagnostic => diagnostic.Id == AsyncApiAttributeAnalyzer.InvalidReplyConfigurationDiagnosticId);
        }

        [Fact]
        public async Task AnalyzeAsync_AllowsMultipleReplyMessagesWithValidReferences()
        {
            const string source = """
using Saunter.AttributeProvider.Attributes;

[AsyncApi]
public class OrdersApi
{
    [Channel("orders", "orders.created")]
    [SendOperation(Reply = "ordersReply")]
    [ReplyMessage(typeof(string), MessageId = "ordersAccepted", PayloadSchemaId = "ordersAcceptedSchema")]
    [ReplyMessage(typeof(int), MessageId = "ordersRejected", PayloadSchemaId = "ordersRejectedSchema")]
    public void Publish() { }
}
""";

            var diagnostics = await AnalyzeAsync(source);

            diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == AsyncApiAttributeAnalyzer.InvalidReplyConfigurationDiagnosticId);
            diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == AsyncApiAttributeAnalyzer.InvalidReferenceNameDiagnosticId);
        }

        [Fact]
        public async Task AnalyzeAsync_DetectsSingleReplySchemaIdWithoutPayloadType()
        {
            const string source = """
using Saunter.AttributeProvider.Attributes;

[AsyncApi]
public class OrdersApi
{
    [Channel("orders", "orders.created")]
    [SendOperation(Reply = "ordersReply", ReplyMessagePayloadSchemaId = "replySchema")]
    public void Publish() { }
}
""";

            var diagnostics = await AnalyzeAsync(source);

            diagnostics.ShouldContain(diagnostic => diagnostic.Id == AsyncApiAttributeAnalyzer.InvalidReplyConfigurationDiagnosticId);
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
