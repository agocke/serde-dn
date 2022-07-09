
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;

namespace Serde.Test
{
    public static class GeneratorTestUtils
    {
        public static Task VerifyDiagnostics(
            string src,
            params DiagnosticResult[] diagnostics)
            => VerifyGeneratedCode(src, System.Array.Empty<(string, string)>(), diagnostics);

        public static Task VerifyGeneratedCode(
            string src,
            string typeName,
            string expected,
            params DiagnosticResult[] diagnostics)
            => VerifyGeneratedCode(src, new[] { (typeName, expected)}, diagnostics);

        public static Task VerifyGeneratedCode(
            string src,
            (string fileName, string expected)[] generated,
            params DiagnosticResult[] diagnostics)
        {
            var verifier = CreateVerifier(src);
            verifier.ExpectedDiagnostics.AddRange(diagnostics);
            foreach (var (fileName, expected) in generated)
            {
                verifier.TestState.GeneratedSources.Add((
                    Path.Combine("SerdeGenerator", $"Serde.{nameof(Generator)}", $"{fileName}.cs"),
                    SourceText.From(expected, Encoding.UTF8))
                );
            }
            return verifier.RunAsync();
        }

        private static CSharpSourceGeneratorTest<Generator, XUnitVerifier> CreateVerifier(string src)
        {
            var verifier = new Verifier()
            {
                TestCode = src,
                ReferenceAssemblies = Config.LatestTfRefs,
            };
            verifier.TestState.AdditionalReferences.Add(typeof(Serde.GenerateSerialize).Assembly);
            return verifier;
        }

        private class Verifier : CSharpSourceGeneratorTest<Generator, XUnitVerifier>
        {
            protected override ParseOptions CreateParseOptions() => new CSharpParseOptions(LanguageVersion.Preview);
        }
    }
}