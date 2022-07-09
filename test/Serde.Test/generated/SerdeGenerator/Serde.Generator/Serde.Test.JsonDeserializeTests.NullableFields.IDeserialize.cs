
#nullable enable
using Serde;

namespace Serde.Test
{
    partial class JsonDeserializeTests
    {
        partial class NullableFields : Serde.IDeserialize<Serde.Test.JsonDeserializeTests.NullableFields>
        {
            static Serde.Test.JsonDeserializeTests.NullableFields Serde.IDeserialize<Serde.Test.JsonDeserializeTests.NullableFields>.Deserialize<D>(ref D deserializer)
            {
                var visitor = new SerdeVisitor();
                var fieldNames = new[]{"S", "Dict"};
                return deserializer.DeserializeType<Serde.Test.JsonDeserializeTests.NullableFields, SerdeVisitor>("NullableFields", fieldNames, visitor);
            }

            private sealed class SerdeVisitor : Serde.IDeserializeVisitor<Serde.Test.JsonDeserializeTests.NullableFields>
            {
                public string ExpectedTypeName => "Serde.Test.JsonDeserializeTests.NullableFields";
                Serde.Test.JsonDeserializeTests.NullableFields Serde.IDeserializeVisitor<Serde.Test.JsonDeserializeTests.NullableFields>.VisitDictionary<D>(ref D d)
                {
                    Serde.Option<string?> s = default;
                    Serde.Option<System.Collections.Generic.Dictionary<string, string?>> dict = default;
                    while (d.TryGetNextKey<string, StringWrap>(out string? key))
                    {
                        switch (key)
                        {
                            case "S":
                                s = d.GetNextValue<string?, NullableRefWrap.DeserializeImpl<string, StringWrap>>();
                                break;
                            case "Dict":
                                dict = d.GetNextValue<System.Collections.Generic.Dictionary<string, string?>, DictWrap.DeserializeImpl<string, StringWrap, string?, NullableRefWrap.DeserializeImpl<string, StringWrap>>>();
                                break;
                            default:
                                break;
                        }
                    }

                    var newType = new Serde.Test.JsonDeserializeTests.NullableFields()
                    {S = s.GetValueOrThrow("S"), Dict = dict.GetValueOrThrow("Dict"), };
                    return newType;
                }
            }
        }
    }
}