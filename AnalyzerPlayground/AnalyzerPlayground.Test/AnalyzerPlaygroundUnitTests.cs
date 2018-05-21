using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace AnalyzerPlayground.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void TestImplicitCast()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class Test
        {
            public Test(string _) { }
            public static implicit operator Test(string _) => null;
        }

        class Actual
        {
            public Actual()
            {
                Test test = ""Hello"";
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "AnalyzerPlayground",
                Message = "Do not use implicit cast operator, use constructor instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 14, 29)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
    namespace ConsoleApplication1
    {
        class Test
        {
            public Test(string _) { }
            public static implicit operator Test(string _) => null;
        }

        class Actual
        {
            public Actual()
            {
                Test test = new ConsoleApplication1.Test(""Hello"");
            }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new AnalyzerPlaygroundCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AnalyzerPlaygroundAnalyzer();
        }
    }
}