using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace AnalyzerPlayground
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AnalyzerPlaygroundAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AnalyzerPlayground";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(AnalyzeConversionOperator, OperationKind.Conversion);
        }

        private void AnalyzeConversionOperator(OperationAnalysisContext obj)
        {
            var conversionOperator = obj.Operation as IConversionOperation;
            if (conversionOperator.IsImplicit && conversionOperator.OperatorMethod != null)
            {
                obj.ReportDiagnostic(Diagnostic.Create(Rule, conversionOperator.Syntax.GetLocation()));
            }
        }
    }
}