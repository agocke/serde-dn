
#nullable enable
using Serde;

namespace Serde.Test
{
    partial class MemberFormatTests
    {
        partial struct S : Serde.IDeserialize<Serde.Test.MemberFormatTests.S>
        {
            static Serde.Test.MemberFormatTests.S Serde.IDeserialize<Serde.Test.MemberFormatTests.S>.Deserialize<D>(ref D deserializer)
            {
                var visitor = new SerdeVisitor();
                var fieldNames = new[]{"One", "TwoWord"};
                return deserializer.DeserializeType<Serde.Test.MemberFormatTests.S, SerdeVisitor>("S", fieldNames, visitor);
            }

            private sealed class SerdeVisitor : Serde.IDeserializeVisitor<Serde.Test.MemberFormatTests.S>
            {
                public string ExpectedTypeName => "Serde.Test.MemberFormatTests.S";
                Serde.Test.MemberFormatTests.S Serde.IDeserializeVisitor<Serde.Test.MemberFormatTests.S>.VisitDictionary<D>(ref D d)
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

                    Serde.Test.MemberFormatTests.S newType = new Serde.Test.MemberFormatTests.S()
                    {One = one.GetValueOrThrow("One"), TwoWord = twoword.GetValueOrThrow("TwoWord"), };
                    return newType;
                }
            }
        }
    }
}