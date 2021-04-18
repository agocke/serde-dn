
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Serde.Test
{
    public class GeneratorTests
    {
        [Fact]
        public Task AllInOne()
        {
            var curPath = GetPath();
            var allInOnePath = Path.Combine(Path.GetDirectoryName(curPath), "AllInOneSrc.cs");

            var src = File.ReadAllText(allInOnePath);
            // Add [GenerateSerde] to the class
            src = src.Replace("internal partial class AllInOne", @"[GenerateSerde] internal partial class AllInOne");
            var expected = @"
internal partial class AllInOne : Serde.ISerialize
{
    public void Serde.ISerialize.Serialize<TSerializer, TSerializeStruct>(TSerializer serializer)
        where TSerializer : Serde.ISerializer<TSerializeStruct> where TSerializeStruct : Serde.ISerializeType
    {
        var type = serializer.SerializeStruct(""AllInOne"", 8);
        type.SerializeField(""ByteField"", new ByteWrap(ByteField));
        type.SerializeField(""UShortField"", new UInt16Wrap(UShortField));
        type.SerializeField(""UIntField"", new UInt32Wrap(UIntField));
        type.SerializeField(""ULongField"", new UInt64Wrap(ULongField));
        type.SerializeField(""SByteField"", SByteField);
        type.SerializeField(""ShortField"", ShortField);
        type.SerializeField(""IntField"", IntField);
        type.SerializeField(""LongField"", LongField);
        type.End();
    }
}";
            return VerifyGeneratedCode(src, "AllInOne", expected);

            static string GetPath([CallerFilePath] string path = "") => path;
        }

        [Fact]
        public Task Rgb()
        {
            var src = @"
using Serde;

[GenerateSerde]
partial class Rgb
{
    public byte Red;
    public byte Green;
    public byte Blue;
}";
            var expected = @"
partial class Rgb : Serde.ISerialize
{
    public void Serde.ISerialize.Serialize<TSerializer, TSerializeStruct>(TSerializer serializer)
        where TSerializer : Serde.ISerializer<TSerializeStruct> where TSerializeStruct : Serde.ISerializeType
    {
        var type = serializer.SerializeStruct(""Rgb"", 3);
        type.SerializeField(""Red"", new ByteWrap(Red));
        type.SerializeField(""Green"", new ByteWrap(Green));
        type.SerializeField(""Blue"", new ByteWrap(Blue));
        type.End();
    }
}";
            return VerifyGeneratedCode(src, "Rgb", expected);
        }

        private Task VerifyGeneratedCode(string src, string typeName, string expected)
        {
            var verifier = new CSharpSourceGeneratorTest<SerdeGenerator, XUnitVerifier>()
            {
                TestCode = src,
                ReferenceAssemblies = Config.LatestTfRefs,
            };
            verifier.TestState.AdditionalReferences.Add(typeof(Serde.GenerateSerdeAttribute).Assembly);
            verifier.TestState.GeneratedSources.Add((
                Path.Combine("SerdeGenerator", "Serde.SerdeGenerator", $"{typeName}.ISerialize.cs"),
                SourceText.From(expected, Encoding.UTF8))
            );
            return verifier.RunAsync();
        }
    }
}