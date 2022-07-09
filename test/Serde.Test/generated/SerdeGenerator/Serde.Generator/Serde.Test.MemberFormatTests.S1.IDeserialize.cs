
#nullable enable
using Serde;

namespace Serde.Test
{
    partial class MemberFormatTests
    {
        partial struct S1 : Serde.IDeserialize<Serde.Test.MemberFormatTests.S1>
        {
            static Serde.Test.MemberFormatTests.S1 Serde.IDeserialize<Serde.Test.MemberFormatTests.S1>.Deserialize<D>(ref D deserializer)
            {
                var visitor = new SerdeVisitor();
                var fieldNames = new[]{"One", "TwoWord"};
                return deserializer.DeserializeType<Serde.Test.MemberFormatTests.S1, SerdeVisitor>("S1", fieldNames, visitor);
            }

            private sealed class SerdeVisitor : Serde.IDeserializeVisitor<Serde.Test.MemberFormatTests.S1>
            {
                public string ExpectedTypeName => "Serde.Test.MemberFormatTests.S1";
                Serde.Test.MemberFormatTests.S1 Serde.IDeserializeVisitor<Serde.Test.MemberFormatTests.S1>.VisitDictionary<D>(ref D d)
                {
                    Serde.Option<int> one = default;
                    Serde.Option<int> twoword = default;
                    while (d.TryGetNextKey<string, StringWrap>(out string? key))
                    {
                        switch (key)
                        {
                            case "one":
                                one = d.GetNextValue<int, Int32Wrap>();
                                break;
                            case "twoWord":
                                twoword = d.GetNextValue<int, Int32Wrap>();
                                break;
                            default:
                                break;
                        }
                    }

                    var newType = new Serde.Test.MemberFormatTests.S1()
                    {One = one.GetValueOrThrow("One"), TwoWord = twoword.GetValueOrThrow("TwoWord"), };
                    return newType;
                }
            }
        }
    }
}