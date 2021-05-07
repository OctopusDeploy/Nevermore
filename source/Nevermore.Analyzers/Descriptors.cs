using Microsoft.CodeAnalysis;
// ReSharper disable InconsistentNaming

namespace Nevermore.Analyzers
{
    internal static class Descriptors
    {
        internal static readonly DiagnosticDescriptor NV0001NevermoreWhereExpressionError = Create(
            "NV0001",
            "Nevermore LINQ expression",
            "Nevermore LINQ support will not be able to translate this expression: {0}",
            "Nevermore",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: "https://github.com/OctopusDeploy/Nevermore/wiki/Querying");

        internal static readonly DiagnosticDescriptor NV0005NevermoreEmbeddedSqlWarning = Create(
            "NV0005",
            "Nevermore embedded SQL",
            "{0}",
            "Nevermore",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: "https://github.com/OctopusDeploy/Nevermore/wiki/Querying");

        internal static readonly DiagnosticDescriptor NV0006NevermoreEmbeddedSqlError = Create(
            "NV0006",
            "Nevermore embedded SQL",
            "{0}",
            "Nevermore",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: "https://github.com/OctopusDeploy/Nevermore/wiki/Querying");

        internal static readonly DiagnosticDescriptor NV0007NevermoreSqlInjectionError = Create(
            "NV0007",
            "Nevermore SQL injection",
            "{0}",
            "Nevermore",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: "https://github.com/OctopusDeploy/Nevermore/wiki/Querying");

        internal static readonly DiagnosticDescriptor NV0008NevermoreDisposableTransactionCreated = Create(
            "NV0008",
            "Nevermore disposable transaction created",
            "{0}",
            "Nevermore",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: "");

        /// <summary>
        /// Create a DiagnosticDescriptor, which provides description about a <see cref="Diagnostic" />.
        /// </summary>
        /// <param name="id">A unique identifier for the diagnostic. For example, code analysis diagnostic ID "CA1001".</param>
        /// <param name="title">A short title describing the diagnostic. For example, for CA1001: "Types that own disposable fields should be disposable".</param>
        /// <param name="messageFormat">A format message string, which can be passed as the first argument to <see cref="string.Format(string,object[])" /> when creating the diagnostic message with this descriptor.
        /// For example, for CA1001: "Implement IDisposable on '{0}' because it creates members of the following IDisposable types: '{1}'.</param>
        /// <param name="category">The category of the diagnostic (like Design, Naming etc.). For example, for CA1001: "Microsoft.Design".</param>
        /// <param name="defaultSeverity">Default severity of the diagnostic.</param>
        /// <param name="isEnabledByDefault">True if the diagnostic is enabled by default.</param>
        /// <param name="description">An optional longer description of the diagnostic.</param>
        /// <param name="helpLinkUri">An optional hyperlink that provides a more detailed description regarding the diagnostic.</param>
        /// <param name="customTags">Optional custom tags for the diagnostic. See <see cref="WellKnownDiagnosticTags" /> for some well known tags.</param>
        static DiagnosticDescriptor Create(
            string id,
            string title,
            string messageFormat,
            string category,
            DiagnosticSeverity defaultSeverity,
            bool isEnabledByDefault,
            string description,
            string helpLinkUri,
            params string[] customTags)
        {
            return new DiagnosticDescriptor(
                id: id,
                title: title,
                messageFormat: messageFormat,
                category: category,
                defaultSeverity: defaultSeverity,
                isEnabledByDefault: isEnabledByDefault,
                description: description,
                helpLinkUri: helpLinkUri,
                customTags: customTags);
        }
    }
}