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

        private static readonly Regex s_referenceNamePattern = new("^[A-Za-z0-9._-]+$", RegexOptions.Compiled);
        private static readonly Regex s_channelParameterNamePattern = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);
        private static readonly Regex s_channelAddressExpressionPattern = new(@"\{([A-Za-z0-9_-]+)\}", RegexOptions.Compiled);

        private static readonly DiagnosticDescriptor s_duplicateOperationId = new(
            DuplicateOperationIdDiagnosticId,
            "Duplicate AsyncAPI operation id",
            "Operation id '{0}' is already used elsewhere. Use a unique OperationId or rely on member-name inference.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor s_invalidExternalDocs = new(
            InvalidExternalDocsDiagnosticId,
            "Invalid AsyncAPI external docs URL",
            "ExternalDocs value '{0}' must be a valid absolute URI",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor s_channelParameterMismatch = new(
            ChannelParameterMismatchDiagnosticId,
            "Channel parameter does not match address",
            "Channel parameter '{0}' is not present in address '{1}'. Remove the parameter or add '{{{0}}}' to the address.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor s_invalidReferenceName = new(
            InvalidReferenceNameDiagnosticId,
            "Invalid AsyncAPI reference name",
            "{0} value '{1}' is not a valid AsyncAPI component/server name. Use only letters, digits, '.', '-', or '_'.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor s_orphanedAnnotation = new(
            OrphanedAnnotationDiagnosticId,
            "Annotation is missing surrounding AsyncAPI context",
            "{0} is used without the required surrounding AsyncAPI context: {1}",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            s_duplicateOperationId,
            s_invalidExternalDocs,
            s_channelParameterMismatch,
            s_invalidReferenceName,
            s_orphanedAnnotation);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(startContext =>
            {
                var seenOperationIds = new ConcurrentDictionary<string, Location>(StringComparer.Ordinal);
                startContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeAttribute(syntaxContext, seenOperationIds),
                    Microsoft.CodeAnalysis.CSharp.SyntaxKind.Attribute);
            });
        }

        private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context, ConcurrentDictionary<string, Location> seenOperationIds)
        {
            var attributeSyntax = (AttributeSyntax)context.Node;
            if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
            {
                return;
            }

            var attributeType = attributeSymbol.ContainingType;
            var attributeName = attributeType.Name;
            if (attributeName is "SendOperationAttribute" or "ReceiveOperationAttribute")
            {
                AnalyzeOperationAttribute(context, attributeSyntax, seenOperationIds);
                return;
            }

            if (attributeName == "MessageAttribute")
            {
                AnalyzeMessageAttribute(context, attributeSyntax);
                return;
            }

            if (attributeName == "ChannelAttribute")
            {
                AnalyzeChannelAttribute(context, attributeSyntax);
                return;
            }

            if (attributeName == "ChannelParameterAttribute")
            {
                AnalyzeChannelParameterAttribute(context, attributeSyntax);
            }
        }

        private static void AnalyzeOperationAttribute(SyntaxNodeAnalysisContext context, AttributeSyntax attributeSyntax, ConcurrentDictionary<string, Location> seenOperationIds)
        {
            foreach (var value in GetNamedStringValues(attributeSyntax, "OperationId"))
            {
                if (!s_referenceNamePattern.IsMatch(value.Value))
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_invalidReferenceName, value.Location, "OperationId", value.Value));
                }

                if (!seenOperationIds.TryAdd(value.Value, value.Location))
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_duplicateOperationId, value.Location, value.Value));
                }
            }

            foreach (var propertyName in new[] { "BindingsRef", "Reply" })
            {
                foreach (var value in GetNamedStringValues(attributeSyntax, propertyName))
                {
                    if (!s_referenceNamePattern.IsMatch(value.Value))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(s_invalidReferenceName, value.Location, propertyName, value.Value));
                    }
                }
            }
        }

        private static void AnalyzeMessageAttribute(SyntaxNodeAnalysisContext context, AttributeSyntax attributeSyntax)
        {
            foreach (var value in GetNamedStringValues(attributeSyntax, "ExternalDocs"))
            {
                if (!Uri.TryCreate(value.Value, UriKind.Absolute, out _))
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_invalidExternalDocs, value.Location, value.Value));
                }
            }

            foreach (var propertyName in new[] { "BindingsRef", "CorrelationId", "MessageId" })
            {
                foreach (var value in GetNamedStringValues(attributeSyntax, propertyName))
                {
                    if (!s_referenceNamePattern.IsMatch(value.Value))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(s_invalidReferenceName, value.Location, propertyName, value.Value));
                    }
                }
            }

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

        private static void AnalyzeChannelAttribute(SyntaxNodeAnalysisContext context, AttributeSyntax attributeSyntax)
        {
            foreach (var value in GetNamedStringValues(attributeSyntax, "BindingsRef"))
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

            foreach (var parameterAttribute in member.GetAttributes().Where(attr => attr.AttributeClass?.Name == "ChannelParameterAttribute"))
            {
                var name = parameterAttribute.ConstructorArguments.FirstOrDefault().Value as string;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var location = parameterAttribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? attributeSyntax.GetLocation();
                if (!s_channelParameterNamePattern.IsMatch(name))
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_invalidReferenceName, location, "ChannelParameter", name));
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

            var hasLocalChannel = member.GetAttributes().Any(attr => attr.AttributeClass?.Name == "ChannelAttribute");
            var hasTypeChannel = member.ContainingType?.GetAttributes().Any(attr => attr.AttributeClass?.Name == "ChannelAttribute") == true;
            if (!hasLocalChannel && !hasTypeChannel)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_orphanedAnnotation,
                    attributeSyntax.GetLocation(),
                    "ChannelParameterAttribute",
                    "Add [Channel] on the method or containing type."));
            }
        }

        private static IEnumerable<(string Value, Location Location)> GetNamedStringValues(AttributeSyntax attributeSyntax, string propertyName)
        {
            if (attributeSyntax.ArgumentList is null)
            {
                yield break;
            }

            foreach (var argument in attributeSyntax.ArgumentList.Arguments.Where(argument => argument.NameEquals?.Name.Identifier.ValueText == propertyName))
            {
                if (argument.Expression is LiteralExpressionSyntax literal && literal.Token.ValueText is string value)
                {
                    yield return (value, literal.GetLocation());
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
            attribute.AttributeClass?.Name is "SendOperationAttribute" or "ReceiveOperationAttribute";

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
