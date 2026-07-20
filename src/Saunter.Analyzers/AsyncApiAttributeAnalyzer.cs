using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Saunter.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AsyncApiAttributeAnalyzer : DiagnosticAnalyzer
    {
        public const string DuplicateOperationIdDiagnosticId = "SAUN001";
        public const string InvalidExternalDocsDiagnosticId = "SAUN002";
        public const string ChannelParameterMismatchDiagnosticId = "SAUN003";
        public const string InvalidReferenceNameDiagnosticId = "SAUN004";
        public const string OrphanedAnnotationDiagnosticId = "SAUN005";
        public const string InvalidChannelParameterNameDiagnosticId = "SAUN006";
        public const string InvalidReplyConfigurationDiagnosticId = "SAUN007";

        private const string HelpLinkBase = "https://github.com/APL-Digital/saunter/blob/development/docs/analyzers.md";
        private const string SaunterAttributeNamespace = "Saunter.AttributeProvider.Attributes";
        private const string SaunterAssemblyName = "Saunter";

        private static readonly Regex s_referenceNamePattern = new("^[A-Za-z0-9._-]+$", RegexOptions.Compiled);
        private static readonly Regex s_channelParameterNamePattern = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);
        private static readonly Regex s_channelAddressExpressionPattern = new(@"\{([A-Za-z0-9_-]+)\}", RegexOptions.Compiled);

        private static readonly DiagnosticDescriptor s_duplicateOperationId = new(
            DuplicateOperationIdDiagnosticId,
            "Duplicate AsyncAPI operation id",
            "Operation id '{0}' is already used elsewhere. Use a unique OperationId or rely on member-name inference.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: null,
            helpLinkUri: HelpLinkBase + "#saun001",
            WellKnownDiagnosticTags.CompilationEnd);

        private static readonly DiagnosticDescriptor s_invalidExternalDocs = new(
            InvalidExternalDocsDiagnosticId,
            "Invalid AsyncAPI external docs URL",
            "ExternalDocs value '{0}' must be a valid absolute URI",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: null,
            helpLinkUri: HelpLinkBase + "#saun002");

        private static readonly DiagnosticDescriptor s_channelParameterMismatch = new(
            ChannelParameterMismatchDiagnosticId,
            "Channel parameter does not match address",
            "Channel parameter '{0}' is not present in address '{1}'. Remove the parameter or add '{{{0}}}' to the address.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: null,
            helpLinkUri: HelpLinkBase + "#saun003");

        private static readonly DiagnosticDescriptor s_invalidReferenceName = new(
            InvalidReferenceNameDiagnosticId,
            "Invalid AsyncAPI reference name",
            "{0} value '{1}' is not a valid AsyncAPI component/server name. Use only letters, digits, '.', '-', or '_'.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: null,
            helpLinkUri: HelpLinkBase + "#saun004");

        private static readonly DiagnosticDescriptor s_invalidChannelParameterName = new(
            InvalidChannelParameterNameDiagnosticId,
            "Invalid AsyncAPI channel parameter name",
            "ChannelParameter value '{0}' is not a valid AsyncAPI channel parameter name. Use only letters, digits, '-', or '_'.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: null,
            helpLinkUri: HelpLinkBase + "#saun006");

        private static readonly DiagnosticDescriptor s_orphanedAnnotation = new(
            OrphanedAnnotationDiagnosticId,
            "Annotation is missing surrounding AsyncAPI context",
            "{0} is used without the required surrounding AsyncAPI context: {1}",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: null,
            helpLinkUri: HelpLinkBase + "#saun005");

        private static readonly DiagnosticDescriptor s_invalidReplyConfiguration = new(
            InvalidReplyConfigurationDiagnosticId,
            "Invalid AsyncAPI operation reply configuration",
            "{0}",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: null,
            helpLinkUri: HelpLinkBase + "#saun007");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            s_duplicateOperationId,
            s_invalidExternalDocs,
            s_channelParameterMismatch,
            s_invalidReferenceName,
            s_orphanedAnnotation,
            s_invalidChannelParameterName,
            s_invalidReplyConfiguration);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(startContext =>
            {
                // Collect occurrences during (unordered, concurrent) node analysis, then
                // report duplicates once at compilation end so results are deterministic
                // and don't depend on visitation order or stale incremental state.
                var operationIdOccurrences = new ConcurrentBag<(string Value, Location Location)>();
                startContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeAttribute(syntaxContext, operationIdOccurrences),
                    Microsoft.CodeAnalysis.CSharp.SyntaxKind.Attribute);
                startContext.RegisterCompilationEndAction(endContext => ReportDuplicateOperationIds(endContext, operationIdOccurrences));
            });
        }

        private static void ReportDuplicateOperationIds(CompilationAnalysisContext context, ConcurrentBag<(string Value, Location Location)> operationIdOccurrences)
        {
            foreach (var group in operationIdOccurrences.GroupBy(occurrence => occurrence.Value, StringComparer.Ordinal))
            {
                var ordered = group
                    .OrderBy(occurrence => occurrence.Location.SourceTree?.FilePath, StringComparer.Ordinal)
                    .ThenBy(occurrence => occurrence.Location.SourceSpan.Start)
                    .ToArray();

                foreach (var duplicate in ordered.Skip(1))
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_duplicateOperationId, duplicate.Location, duplicate.Value));
                }
            }
        }

        private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context, ConcurrentBag<(string Value, Location Location)> operationIdOccurrences)
        {
            var attributeSyntax = (AttributeSyntax)context.Node;
            if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
            {
                return;
            }

            var attributeType = attributeSymbol.ContainingType;
            if (IsSaunterAttribute(attributeType, "SendOperationAttribute")
                || IsSaunterAttribute(attributeType, "ReceiveOperationAttribute"))
            {
                AnalyzeOperationAttribute(context, attributeSyntax, operationIdOccurrences);
                return;
            }

            if (IsSaunterAttribute(attributeType, "MessageAttribute"))
            {
                AnalyzeMessageAttribute(context, attributeSyntax);
                return;
            }

            if (IsSaunterAttribute(attributeType, "ReplyMessageAttribute"))
            {
                AnalyzeReplyMessageAttribute(context, attributeSyntax);
                return;
            }

            if (IsSaunterAttribute(attributeType, "ChannelAttribute"))
            {
                AnalyzeChannelAttribute(context, attributeSyntax);
                return;
            }

            if (IsSaunterAttribute(attributeType, "ChannelParameterAttribute"))
            {
                AnalyzeChannelParameterAttribute(context, attributeSyntax);
            }
        }

        private static void AnalyzeOperationAttribute(SyntaxNodeAnalysisContext context, AttributeSyntax attributeSyntax, ConcurrentBag<(string Value, Location Location)> operationIdOccurrences)
        {
            foreach (var value in GetNamedStringValues(context, attributeSyntax, "OperationId"))
            {
                if (!s_referenceNamePattern.IsMatch(value.Value))
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_invalidReferenceName, value.Location, "OperationId", value.Value));
                }

                operationIdOccurrences.Add((value.Value, value.Location));
            }

            foreach (var propertyName in new[] { "BindingsRef", "Reply", "ReplyMessageId", "ReplyMessagePayloadSchemaId" })
            {
                foreach (var value in GetNamedStringValues(context, attributeSyntax, propertyName))
                {
                    if (!s_referenceNamePattern.IsMatch(value.Value))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(s_invalidReferenceName, value.Location, propertyName, value.Value));
                    }
                }
            }

            AnalyzeReplyConfiguration(context, attributeSyntax);
        }

        private static void AnalyzeReplyConfiguration(SyntaxNodeAnalysisContext context, AttributeSyntax attributeSyntax)
        {
            var hasReply = GetNamedStringValues(context, attributeSyntax, "Reply")
                .Any(value => !string.IsNullOrWhiteSpace(value.Value));
            var replyChannelAddress = GetNamedArgumentLocation(attributeSyntax, "ReplyChannelAddress");
            var replyAddressLocation = GetNamedArgumentLocation(attributeSyntax, "ReplyAddressLocation");
            var replyMessagePayloadType = GetNamedArgumentLocation(attributeSyntax, "ReplyMessagePayloadType");
            var replyMetadata = new[]
            {
                (Name: "ReplyMessagePayloadSchemaId", Location: GetNamedNonBlankStringArgumentLocation(context, attributeSyntax, "ReplyMessagePayloadSchemaId")),
                (Name: "ReplyMessageId", Location: GetNamedNonBlankStringArgumentLocation(context, attributeSyntax, "ReplyMessageId")),
                (Name: "ReplyMessageName", Location: GetNamedNonBlankStringArgumentLocation(context, attributeSyntax, "ReplyMessageName")),
                (Name: "ReplyMessageTitle", Location: GetNamedNonBlankStringArgumentLocation(context, attributeSyntax, "ReplyMessageTitle")),
            };

            if (replyChannelAddress is not null && replyAddressLocation is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_invalidReplyConfiguration,
                    replyAddressLocation,
                    "ReplyChannelAddress and ReplyAddressLocation are mutually exclusive. Remove one of them so the reply channel is either explicitly addressed or dynamically addressed."));
            }

            if (replyMessagePayloadType is null)
            {
                foreach (var metadata in replyMetadata.Where(metadata => metadata.Location is not null))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        s_invalidReplyConfiguration,
                        metadata.Location,
                        $"{metadata.Name} requires ReplyMessagePayloadType. Set the payload type or move the metadata to a [ReplyMessage] attribute."));
                }
            }

            if (hasReply)
            {
                return;
            }

            if (replyChannelAddress is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_invalidReplyConfiguration,
                    replyChannelAddress,
                    "ReplyChannelAddress requires a Reply channel id. Set Reply to the generated reply channel id."));
            }

            if (replyAddressLocation is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_invalidReplyConfiguration,
                    replyAddressLocation,
                    "ReplyAddressLocation requires a Reply channel id. Set Reply to the reply channel id or remove the reply address metadata."));
            }

            if (replyMessagePayloadType is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_invalidReplyConfiguration,
                    replyMessagePayloadType,
                    "ReplyMessagePayloadType requires a Reply channel id. Set Reply to the reply channel id or remove the reply payload type."));
            }

            foreach (var metadata in replyMetadata.Where(metadata => metadata.Location is not null))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_invalidReplyConfiguration,
                    metadata.Location,
                    $"{metadata.Name} requires a Reply channel id. Set Reply to the reply channel id or remove the reply metadata."));
            }
        }

        private static Location? GetNamedArgumentLocation(AttributeSyntax attributeSyntax, string propertyName) =>
            attributeSyntax.ArgumentList?.Arguments
                .FirstOrDefault(argument => argument.NameEquals?.Name.Identifier.ValueText == propertyName)
                ?.GetLocation();

        private static Location? GetNamedNonBlankStringArgumentLocation(
            SyntaxNodeAnalysisContext context,
            AttributeSyntax attributeSyntax,
            string propertyName)
        {
            return GetNamedStringValues(context, attributeSyntax, propertyName)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value.Value))
                .Location;
        }

        private static void AnalyzeMessageAttribute(SyntaxNodeAnalysisContext context, AttributeSyntax attributeSyntax)
        {
            AnalyzeMessageMetadata(context, attributeSyntax);

            if (context.ContainingSymbol is not IMethodSymbol method)
            {
                return;
            }

            var hasLocalOperation = method.GetAttributes().Any(IsOperationAttribute);
            var hasTypeOperation = method.ContainingType.GetAttributes().Any(IsOperationAttribute);
            if (!hasLocalOperation && !hasTypeOperation)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_orphanedAnnotation,
                    attributeSyntax.GetLocation(),
                    "MessageAttribute",
                    "Add [SendOperation] or [ReceiveOperation] on the method or containing type."));
            }
        }

        private static void AnalyzeReplyMessageAttribute(SyntaxNodeAnalysisContext context, AttributeSyntax attributeSyntax)
        {
            AnalyzeMessageMetadata(context, attributeSyntax);

            if (context.ContainingSymbol is not ISymbol member)
            {
                return;
            }

            var operationAttributes = member.GetAttributes().Where(IsOperationAttribute).ToArray();
            if (member is IMethodSymbol method)
            {
                if (operationAttributes.Length == 0)
                {
                    operationAttributes = method.ContainingType.GetAttributes().Where(IsOperationAttribute).ToArray();
                }
            }

            if (operationAttributes.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_orphanedAnnotation,
                    attributeSyntax.GetLocation(),
                    "ReplyMessageAttribute",
                    "Add [SendOperation] or [ReceiveOperation] with Reply set on the method or containing type."));
                return;
            }

            var replyOperations = operationAttributes.Where(HasReplyChannelId).ToArray();
            if (replyOperations.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_invalidReplyConfiguration,
                    attributeSyntax.GetLocation(),
                    "ReplyMessageAttribute requires a Reply channel id. Set Reply on the surrounding operation or remove the reply message."));
            }
            else if (replyOperations.Length > 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_invalidReplyConfiguration,
                    attributeSyntax.GetLocation(),
                    "ReplyMessageAttribute is ambiguous because multiple surrounding operations configure Reply. Move the operations to separate members or use the operation-specific ReplyMessagePayloadType properties."));
            }
        }

        private static void AnalyzeMessageMetadata(SyntaxNodeAnalysisContext context, AttributeSyntax attributeSyntax)
        {
            foreach (var value in GetNamedStringValues(context, attributeSyntax, "ExternalDocs"))
            {
                if (!Uri.TryCreate(value.Value, UriKind.Absolute, out _))
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_invalidExternalDocs, value.Location, value.Value));
                }
            }

            foreach (var propertyName in new[] { "BindingsRef", "CorrelationId", "MessageId", "PayloadSchemaId" })
            {
                foreach (var value in GetNamedStringValues(context, attributeSyntax, propertyName))
                {
                    if (!s_referenceNamePattern.IsMatch(value.Value))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(s_invalidReferenceName, value.Location, propertyName, value.Value));
                    }
                }
            }
        }

        private static bool HasReplyChannelId(AttributeData attribute)
        {
            return attribute.NamedArguments.Any(argument =>
                argument.Key == "Reply"
                && argument.Value.Value is string reply
                && !string.IsNullOrWhiteSpace(reply));
        }

        private static void AnalyzeChannelAttribute(SyntaxNodeAnalysisContext context, AttributeSyntax attributeSyntax)
        {
            foreach (var value in GetNamedStringValues(context, attributeSyntax, "BindingsRef"))
            {
                if (!s_referenceNamePattern.IsMatch(value.Value))
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_invalidReferenceName, value.Location, "BindingsRef", value.Value));
                }
            }

            foreach (var value in GetNamedArrayStringValues(attributeSyntax, "Servers"))
            {
                if (!s_referenceNamePattern.IsMatch(value.Value))
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_invalidReferenceName, value.Location, "Servers", value.Value));
                }
            }

            var address = GetChannelAddressLiteral(attributeSyntax);
            if (address is null)
            {
                return;
            }

            var expressionNames = GetChannelAddressParameterNames(address);
            if (context.ContainingSymbol is not ISymbol member)
            {
                return;
            }

            foreach (var parameterAttribute in member.GetAttributes().Where(attr => IsSaunterAttribute(attr.AttributeClass, "ChannelParameterAttribute")))
            {
                var name = parameterAttribute.ConstructorArguments.FirstOrDefault().Value as string;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var location = parameterAttribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? attributeSyntax.GetLocation();
                if (!s_channelParameterNamePattern.IsMatch(name))
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_invalidChannelParameterName, location, name));
                    continue;
                }

                if (name is not null && !expressionNames.Contains(name))
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_channelParameterMismatch, location, name, address));
                }
            }
        }

        private static void AnalyzeChannelParameterAttribute(SyntaxNodeAnalysisContext context, AttributeSyntax attributeSyntax)
        {
            if (context.ContainingSymbol is not ISymbol member)
            {
                return;
            }

            var hasLocalChannel = member.GetAttributes().Any(attr => IsSaunterAttribute(attr.AttributeClass, "ChannelAttribute"));
            var hasTypeChannel = member.ContainingType?.GetAttributes().Any(attr => IsSaunterAttribute(attr.AttributeClass, "ChannelAttribute")) == true;
            if (!hasLocalChannel && !hasTypeChannel)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_orphanedAnnotation,
                    attributeSyntax.GetLocation(),
                    "ChannelParameterAttribute",
                    "Add [Channel] on the method or containing type."));
            }
        }

        private static IEnumerable<(string Value, Location Location)> GetNamedStringValues(
            SyntaxNodeAnalysisContext context,
            AttributeSyntax attributeSyntax,
            string propertyName)
        {
            if (attributeSyntax.ArgumentList is null)
            {
                yield break;
            }

            foreach (var argument in attributeSyntax.ArgumentList.Arguments.Where(argument => argument.NameEquals?.Name.Identifier.ValueText == propertyName))
            {
                var constant = context.SemanticModel.GetConstantValue(argument.Expression, context.CancellationToken);
                if (constant.HasValue && constant.Value is string value)
                {
                    yield return (value, argument.Expression.GetLocation());
                }
            }
        }

        private static IEnumerable<(string Value, Location Location)> GetNamedArrayStringValues(AttributeSyntax attributeSyntax, string propertyName)
        {
            if (attributeSyntax.ArgumentList is null)
            {
                yield break;
            }

            foreach (var argument in attributeSyntax.ArgumentList.Arguments.Where(argument => argument.NameEquals?.Name.Identifier.ValueText == propertyName))
            {
                if (argument.Expression is ArrayCreationExpressionSyntax arrayCreation)
                {
                    foreach (var initializerValue in GetArrayInitializerValues(arrayCreation.Initializer))
                    {
                        yield return initializerValue;
                    }
                }
                else if (argument.Expression is ImplicitArrayCreationExpressionSyntax implicitArray)
                {
                    foreach (var initializerValue in GetArrayInitializerValues(implicitArray.Initializer))
                    {
                        yield return initializerValue;
                    }
                }
                else if (argument.Expression is InitializerExpressionSyntax initializer)
                {
                    foreach (var initializerValue in GetArrayInitializerValues(initializer))
                    {
                        yield return initializerValue;
                    }
                }
            }
        }

        private static IEnumerable<(string Value, Location Location)> GetArrayInitializerValues(InitializerExpressionSyntax? initializer)
        {
            if (initializer is null)
            {
                yield break;
            }

            foreach (var expression in initializer.Expressions.OfType<LiteralExpressionSyntax>())
            {
                if (expression.Token.ValueText is string value)
                {
                    yield return (value, expression.GetLocation());
                }
            }
        }

        private static string? GetChannelAddressLiteral(AttributeSyntax attributeSyntax)
        {
            if (attributeSyntax.ArgumentList is null)
            {
                return null;
            }

            var positionalArguments = attributeSyntax.ArgumentList.Arguments.Where(argument => argument.NameEquals is null).ToArray();
            if (positionalArguments.Length == 1 && positionalArguments[0].Expression is LiteralExpressionSyntax singleLiteral)
            {
                return singleLiteral.Token.ValueText;
            }

            if (positionalArguments.Length >= 2 && positionalArguments[1].Expression is LiteralExpressionSyntax addressLiteral)
            {
                return addressLiteral.Token.ValueText;
            }

            return null;
        }

        private static bool IsOperationAttribute(AttributeData attribute) =>
            IsSaunterAttribute(attribute.AttributeClass, "SendOperationAttribute")
            || IsSaunterAttribute(attribute.AttributeClass, "ReceiveOperationAttribute");

        private static bool IsSaunterAttribute(INamedTypeSymbol? attributeType, string attributeName)
        {
            return attributeType is not null
                && attributeType.Name == attributeName
                && attributeType.ContainingNamespace.ToDisplayString() == SaunterAttributeNamespace
                && attributeType.ContainingAssembly.Name == SaunterAssemblyName;
        }

        private static HashSet<string> GetChannelAddressParameterNames(string address)
        {
            if (address.Contains('?') || address.Contains('#'))
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            var matches = s_channelAddressExpressionPattern.Matches(address);
            return matches
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(match => match.Groups[1].Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Aggregate(new HashSet<string>(StringComparer.Ordinal), (set, value) =>
                {
                    set.Add(value);
                    return set;
                });
        }
    }
}
