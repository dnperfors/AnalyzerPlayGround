using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnalyzerPlayground
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AnalyzerPlaygroundCodeFixProvider)), Shared]
    public class AnalyzerPlaygroundCodeFixProvider : CodeFixProvider
    {
        private const string title = "Replace implicit cast operator";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AnalyzerPlaygroundAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindNode(diagnosticSpan);

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => ReplaceImplicitCastOperatorAsync(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> ReplaceImplicitCastOperatorAsync(Document document, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            // Compute new uppercase name.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var operation = semanticModel.GetOperation(syntaxNode, cancellationToken);

            while (operation != null && operation.Kind != OperationKind.Conversion)
            {
                operation = operation.Parent;
            }

            if (operation != null)
            {
                IConversionOperation conversionOperation = operation as IConversionOperation;
                var returntype = conversionOperation.OperatorMethod.ReturnType;
                var constructors = returntype.GetMembers().OfType<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Constructor && x.Parameters.Length == conversionOperation.OperatorMethod.Parameters.Length);
                var constructor = constructors.SingleOrDefault(x => x.Parameters.Select(p => p.Type).SequenceEqual(conversionOperation.OperatorMethod.Parameters.Select(p => p.Type)));
                if (constructor != null)
                {
                    // Produce a new solution that has all references to that type renamed, including the declaration.
                    var originalSolution = document.Project.Solution;
                    var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken);
                    var generator = documentEditor.Generator;

                    var newNode = generator.ObjectCreationExpression(SyntaxFactory.ParseTypeName(conversionOperation.OperatorMethod.ReturnType.ToDisplayString()), conversionOperation.Operand.Syntax);
                    var root = await document.GetSyntaxRootAsync(cancellationToken);
                    var newRoot = root.ReplaceNode(syntaxNode, newNode);
                    return document.WithSyntaxRoot(newRoot);
                }
            }

            return document;
        }

        private class Test
        {
            public Test(string _)
            {
            }

            public static implicit operator Test(string _) => null;
        }

        private class Actual
        {
            public Actual()
            {
                Test test = "Hello";
                Test test2 = new Test("Hello");
            }
        }
    }
}