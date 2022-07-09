
using System.Threading.Tasks;
using Serde.Json;
using Xunit;
using static Serde.Test.GeneratorTestUtils;

namespace Serde.Test
{
    public partial class MemberFormatTests
    {

[GenerateSerde]
[SerdeTypeOptions(MemberFormat = MemberFormat.CamelCase)]
partial struct S1
{
    public int One { get; set; }
    public int TwoWord { get; set; }
}
        [Fact]
        public void CamelCase()
        {
            var s = new S1 { One = 1, TwoWord = 2 };
            var expected = """
{
  "one": 1,
  "twoWord": 2
}
""";
            var text = JsonSerializerTests.PrettyPrint(JsonSerializer.Serialize(s));
            Assert.Equal(expected, text);
            Assert.Equal(s, JsonSerializer.Deserialize<S1>(text));
        }

        [Fact]
        public Task CamelCaseGenerator()
        {
            var src = @"
using Serde;

[GenerateSerialize]
[SerdeTypeOptions(MemberFormat = MemberFormat.CamelCase)]
partial struct S
{
    public int One { get; set; }
    public int TwoWord { get; set; }
}";
            return VerifyGeneratedCode(src, "S.ISerialize", @"
#nullable enable
using Serde;

partial struct S : Serde.ISerialize
{
    void Serde.ISerialize.Serialize(ISerializer serializer)
    {
        var type = serializer.SerializeType(""S"", 2);
        type.SerializeField(""one"", new Int32Wrap(this.One));
        type.SerializeField(""twoWord"", new Int32Wrap(this.TwoWord));
        type.End();
    }
}");
        }

        [GenerateSerde]
        partial struct S2
        {
            [SerdeMemberOptions(Rename = "X")]
            public int One { get; set; }
            [SerdeMemberOptions(Rename = "Y")]
            public int TwoWord { get; set; }
        }

        [Fact]
        public void MemberRename()
        {
            var s = new S2 { One = 1, TwoWord = 2 };
            var text = JsonSerializerTests.PrettyPrint(JsonSerializer.Serialize(s));
            Assert.Equal("""
{
  "X": 1,
  "Y": 2
}
""", text);
            Assert.Equal(s, JsonSerializer.Deserialize<S2>(text));
        }

        [Fact]
        public Task MemberRenameGenerator()
        {
            var src = """
using Serde;

[GenerateSerde]
partial struct S
{
    [SerdeMemberOptions(Rename = "X")]
    public int One { get; set; }
    [SerdeMemberOptions(Rename = "Y")]
    public int TwoWord { get; set; }
}
""";
            return VerifyGeneratedCode(src, new[] {
     ("S.ISerialize", """

#nullable enable
using Serde;

partial struct S : Serde.ISerialize
{
    void Serde.ISerialize.Serialize(ISerializer serializer)
    {
        var type = serializer.SerializeType("S", 2);
        type.SerializeField("X", new Int32Wrap(this.One));
        type.SerializeField("Y", new Int32Wrap(this.TwoWord));
        type.End();
    }
}
"""),
    ("S.IDeserialize", """

#nullable enable
using Serde;

partial struct S : Serde.IDeserialize<S>
{
    static S Serde.IDeserialize<S>.Deserialize<D>(ref D deserializer)
    {
        var visitor = new SerdeVisitor();
        var fieldNames = new[]{"One", "TwoWord"};
        return deserializer.DeserializeType<S, SerdeVisitor>("S", fieldNames, visitor);
    }

    private sealed class SerdeVisitor : Serde.IDeserializeVisitor<S>
    {
        public string ExpectedTypeName => "S";
        S Serde.IDeserializeVisitor<S>.VisitDictionary<D>(ref D d)
        {
            Serde.Option<int> one = default;
            Serde.Option<int> twoword = default;
            while (d.TryGetNextKey<string, StringWrap>(out string? key))
            {
                switch (key)
                {
                    case "X":
                        one = d.GetNextValue<int, Int32Wrap>();
                        break;
                    case "Y":
                        twoword = d.GetNextValue<int, Int32Wrap>();
                        break;
                    default:
                        break;
                }
            }

            var newType = new S()
            {One = one.GetValueOrThrow("One"), TwoWord = twoword.GetValueOrThrow("TwoWord"), };
            return newType;
        }
    }
}
""")
            });
        }
    }
}