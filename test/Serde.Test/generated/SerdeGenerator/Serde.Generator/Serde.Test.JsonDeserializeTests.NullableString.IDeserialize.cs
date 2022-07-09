
#nullable enable
using Serde;

namespace Serde.Test
{
    partial class JsonDeserializeTests
    {
        partial struct NullableString : Serde.IDeserialize<Serde.Test.JsonDeserializeTests.NullableString>
        {
            static Serde.Test.JsonDeserializeTests.NullableString Serde.IDeserialize<Serde.Test.JsonDeserializeTests.NullableString>.Deserialize<D>(ref D deserializer)
            {
                var visitor = new SerdeVisitor();
                var fieldNames = new[]{"S"};
                return deserializer.DeserializeType<Serde.Test.JsonDeserializeTests.NullableString, SerdeVisitor>("NullableString", fieldNames, visitor);
            }

            private sealed class SerdeVisitor : Serde.IDeserializeVisitor<Serde.Test.JsonDeserializeTests.NullableString>
            {
                public string ExpectedTypeName => "Serde.Test.JsonDeserializeTests.NullableString";
                Serde.Test.JsonDeserializeTests.NullableString Serde.IDeserializeVisitor<Serde.Test.JsonDeserializeTests.NullableString>.VisitDictionary<D>(ref D d)
                {
                    Serde.Option<string?> s = default;
                    while (d.TryGetNextKey<string, StringWrap>(out string? key))
                    {
                        switch (key)
                        {
                            case "S":
                                s = d.GetNextValue<string, StringWrap>();
                                break;
                            default:
                                break;
                        }
                    }

                    Serde.Test.JsonDeserializeTests.NullableString newType = new Serde.Test.JsonDeserializeTests.NullableString()
                    {S = s.GetValueOrThrow("S"), };
                    return newType;
                }
            }
        }
    }
}