using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Serde.Json;
using Xunit;

namespace Serde.Test
{
    public class AllInOneJsonTest
    {
        [Fact]
        public Task GeneratorTest()
        {
            var curPath = GetPath();
            var allInOnePath = Path.Combine(Path.GetDirectoryName(curPath)!, "AllInOneSrc.cs");

            var src = File.ReadAllText(allInOnePath);
            var serializeSrc = """

#nullable enable
using Serde;

namespace Serde.Test
{
    partial record AllInOne : Serde.ISerialize
    {
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            var type = serializer.SerializeType("AllInOne", 15);
            type.SerializeField("BoolField", new BoolWrap(this.BoolField));
            type.SerializeField("CharField", new CharWrap(this.CharField));
            type.SerializeField("ByteField", new ByteWrap(this.ByteField));
            type.SerializeField("UShortField", new UInt16Wrap(this.UShortField));
            type.SerializeField("UIntField", new UInt32Wrap(this.UIntField));
            type.SerializeField("ULongField", new UInt64Wrap(this.ULongField));
            type.SerializeField("SByteField", new SByteWrap(this.SByteField));
            type.SerializeField("ShortField", new Int16Wrap(this.ShortField));
            type.SerializeField("IntField", new Int32Wrap(this.IntField));
            type.SerializeField("LongField", new Int64Wrap(this.LongField));
            type.SerializeField("StringField", new StringWrap(this.StringField));
            type.SerializeField("IntArr", new ArrayWrap.SerializeImpl<int, Int32Wrap>(this.IntArr));
            type.SerializeField("NestedArr", new ArrayWrap.SerializeImpl<int[], ArrayWrap.SerializeImpl<int, Int32Wrap>>(this.NestedArr));
            type.SerializeField("IntImm", new ImmutableArrayWrap.SerializeImpl<int, Int32Wrap>(this.IntImm));
            type.SerializeField("Color", new AllInOneColorEnumWrap(this.Color));
            type.End();
        }
    }
}
""";
            var deserializeSrc = @"
#nullable enable
using Serde;

namespace Serde.Test
{
    partial record AllInOne : Serde.IDeserialize<Serde.Test.AllInOne>
    {
        static Serde.Test.AllInOne Serde.IDeserialize<Serde.Test.AllInOne>.Deserialize<D>(ref D deserializer)
        {
            var visitor = new SerdeVisitor();
            var fieldNames = new[]{""BoolField"", ""CharField"", ""ByteField"", ""UShortField"", ""UIntField"", ""ULongField"", ""SByteField"", ""ShortField"", ""IntField"", ""LongField"", ""StringField"", ""IntArr"", ""NestedArr"", ""IntImm"", ""Color""};
            return deserializer.DeserializeType<Serde.Test.AllInOne, SerdeVisitor>(""AllInOne"", fieldNames, visitor);
        }

        private sealed class SerdeVisitor : Serde.IDeserializeVisitor<Serde.Test.AllInOne>
        {
            public string ExpectedTypeName => ""Serde.Test.AllInOne"";
            Serde.Test.AllInOne Serde.IDeserializeVisitor<Serde.Test.AllInOne>.VisitDictionary<D>(ref D d)
            {
                Serde.Option<bool> boolfield = default;
                Serde.Option<char> charfield = default;
                Serde.Option<byte> bytefield = default;
                Serde.Option<ushort> ushortfield = default;
                Serde.Option<uint> uintfield = default;
                Serde.Option<ulong> ulongfield = default;
                Serde.Option<sbyte> sbytefield = default;
                Serde.Option<short> shortfield = default;
                Serde.Option<int> intfield = default;
                Serde.Option<long> longfield = default;
                Serde.Option<string> stringfield = default;
                Serde.Option<int[]> intarr = default;
                Serde.Option<int[][]> nestedarr = default;
                Serde.Option<System.Collections.Immutable.ImmutableArray<int>> intimm = default;
                Serde.Option<Serde.Test.AllInOne.ColorEnum> color = default;
                while (d.TryGetNextKey<string, StringWrap>(out string? key))
                {
                    switch (key)
                    {
                        case ""BoolField"":
                            boolfield = d.GetNextValue<bool, BoolWrap>();
                            break;
                        case ""CharField"":
                            charfield = d.GetNextValue<char, CharWrap>();
                            break;
                        case ""ByteField"":
                            bytefield = d.GetNextValue<byte, ByteWrap>();
                            break;
                        case ""UShortField"":
                            ushortfield = d.GetNextValue<ushort, UInt16Wrap>();
                            break;
                        case ""UIntField"":
                            uintfield = d.GetNextValue<uint, UInt32Wrap>();
                            break;
                        case ""ULongField"":
                            ulongfield = d.GetNextValue<ulong, UInt64Wrap>();
                            break;
                        case ""SByteField"":
                            sbytefield = d.GetNextValue<sbyte, SByteWrap>();
                            break;
                        case ""ShortField"":
                            shortfield = d.GetNextValue<short, Int16Wrap>();
                            break;
                        case ""IntField"":
                            intfield = d.GetNextValue<int, Int32Wrap>();
                            break;
                        case ""LongField"":
                            longfield = d.GetNextValue<long, Int64Wrap>();
                            break;
                        case ""StringField"":
                            stringfield = d.GetNextValue<string, StringWrap>();
                            break;
                        case ""IntArr"":
                            intarr = d.GetNextValue<int[], ArrayWrap.DeserializeImpl<int, Int32Wrap>>();
                            break;
                        case ""NestedArr"":
                            nestedarr = d.GetNextValue<int[][], ArrayWrap.DeserializeImpl<int[], ArrayWrap.DeserializeImpl<int, Int32Wrap>>>();
                            break;
                        case ""IntImm"":
                            intimm = d.GetNextValue<System.Collections.Immutable.ImmutableArray<int>, ImmutableArrayWrap.DeserializeImpl<int, Int32Wrap>>();
                            break;
                        case ""Color"":
                            color = d.GetNextValue<Serde.Test.AllInOne.ColorEnum, AllInOneColorEnumWrap>();
                            break;
                        default:
                            break;
                    }
                }

                var newType = new Serde.Test.AllInOne()
                {BoolField = boolfield.GetValueOrThrow(""BoolField""), CharField = charfield.GetValueOrThrow(""CharField""), ByteField = bytefield.GetValueOrThrow(""ByteField""), UShortField = ushortfield.GetValueOrThrow(""UShortField""), UIntField = uintfield.GetValueOrThrow(""UIntField""), ULongField = ulongfield.GetValueOrThrow(""ULongField""), SByteField = sbytefield.GetValueOrThrow(""SByteField""), ShortField = shortfield.GetValueOrThrow(""ShortField""), IntField = intfield.GetValueOrThrow(""IntField""), LongField = longfield.GetValueOrThrow(""LongField""), StringField = stringfield.GetValueOrThrow(""StringField""), IntArr = intarr.GetValueOrThrow(""IntArr""), NestedArr = nestedarr.GetValueOrThrow(""NestedArr""), IntImm = intimm.GetValueOrThrow(""IntImm""), Color = color.GetValueOrThrow(""Color""), };
                return newType;
            }
        }
    }
}";

            return GeneratorTestUtils.VerifyGeneratedCode(src, new[] {
                ("Serde.AllInOneColorEnumWrap", @"
namespace Serde
{
    internal readonly partial record struct AllInOneColorEnumWrap(Serde.Test.AllInOne.ColorEnum Value);
}"),
                ("Serde.AllInOneColorEnumWrap.ISerialize", """

#nullable enable
using Serde;

namespace Serde
{
    partial record struct AllInOneColorEnumWrap : Serde.ISerialize
    {
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            var name = Value switch
            {
                Serde.Test.AllInOne.ColorEnum.Red => "Red",
                Serde.Test.AllInOne.ColorEnum.Blue => "Blue",
                Serde.Test.AllInOne.ColorEnum.Green => "Green",
                _ => null
            };
            serializer.SerializeEnumValue("ColorEnum", name, new Int32Wrap((int)Value));
        }
    }
}
"""),
                ("Serde.Test.AllInOne.ISerialize", serializeSrc),
                ("Serde.AllInOneColorEnumWrap.IDeserialize", """

#nullable enable
using Serde;

namespace Serde
{
    partial record struct AllInOneColorEnumWrap : Serde.IDeserialize<Serde.Test.AllInOne.ColorEnum>
    {
        static Serde.Test.AllInOne.ColorEnum Serde.IDeserialize<Serde.Test.AllInOne.ColorEnum>.Deserialize<D>(ref D deserializer)
        {
            var visitor = new SerdeVisitor();
            return deserializer.DeserializeString<Serde.Test.AllInOne.ColorEnum, SerdeVisitor>(visitor);
        }

        private sealed class SerdeVisitor : Serde.IDeserializeVisitor<Serde.Test.AllInOne.ColorEnum>
        {
            public string ExpectedTypeName => "Serde.Test.AllInOne.ColorEnum";
            Serde.Test.AllInOne.ColorEnum Serde.IDeserializeVisitor<Serde.Test.AllInOne.ColorEnum>.VisitString(string s)
            {
                Serde.Test.AllInOne.ColorEnum enumValue;
                switch (s)
                {
                    case "Red":
                        enumValue = Serde.Test.AllInOne.ColorEnum.Red;
                        break;
                    case "Blue":
                        enumValue = Serde.Test.AllInOne.ColorEnum.Blue;
                        break;
                    case "Green":
                        enumValue = Serde.Test.AllInOne.ColorEnum.Green;
                        break;
                    default:
                        throw new InvalidDeserializeValueException("Unexpected enum field name: " + s);
                }

                return enumValue;
            }
        }
    }
}
"""),
                ("Serde.Test.AllInOne.IDeserialize", deserializeSrc),
            });

            static string GetPath([CallerFilePath] string path = "") => path;
        }

        private const string Serialized = @"
{
  ""BoolField"": true,
  ""CharField"": ""#"",
  ""ByteField"": 255,
  ""UShortField"": 65535,
  ""UIntField"": 4294967295,
  ""ULongField"": 18446744073709551615,
  ""SByteField"": 127,
  ""ShortField"": 32767,
  ""IntField"": 2147483647,
  ""LongField"": 9223372036854775807,
  ""StringField"": ""StringValue"",
  ""IntArr"": [
    1,
    2,
    3
  ],
  ""NestedArr"": [
    [
      1
    ],
    [
      2
    ]
  ],
  ""IntImm"": [
    1,
    2
  ],
  ""Color"": ""Blue""
}";
        private static readonly AllInOne Deserialized = new AllInOne()
        {
            BoolField = true,
            CharField = '#',
            ByteField = byte.MaxValue,
            UShortField = ushort.MaxValue,
            UIntField = uint.MaxValue,
            ULongField = ulong.MaxValue,

            SByteField = sbyte.MaxValue,
            ShortField = short.MaxValue,
            IntField = int.MaxValue,
            LongField = long.MaxValue,

            StringField = "StringValue",

            IntArr = new[] { 1, 2, 3 },
            NestedArr = new[] { new[] { 1 }, new[] { 2 } },

            IntImm = ImmutableArray.Create<int>(1, 2)
        };

        [Fact]
        public void SerializeTest()
        {
            var actual = JsonSerializerTests.PrettyPrint(JsonSerializer.Serialize(Deserialized));
            Assert.Equal(Serialized.Trim(), actual);
        }

        [Fact]
        public void DeserializeTest()
        {
            var actual = JsonSerializer.Deserialize<AllInOne>(Serialized);
            Assert.Equal(Deserialized, actual);
        }
    }
}
