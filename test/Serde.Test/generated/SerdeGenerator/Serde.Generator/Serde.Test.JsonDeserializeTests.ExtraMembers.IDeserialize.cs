
#nullable enable
using Serde;

namespace Serde.Test
{
    partial class JsonDeserializeTests
    {
        partial struct ExtraMembers : Serde.IDeserialize<Serde.Test.JsonDeserializeTests.ExtraMembers>
        {
            static Serde.Test.JsonDeserializeTests.ExtraMembers Serde.IDeserialize<Serde.Test.JsonDeserializeTests.ExtraMembers>.Deserialize<D>(ref D deserializer)
            {
                var visitor = new SerdeVisitor();
                var fieldNames = new[]{"b"};
                return deserializer.DeserializeType<Serde.Test.JsonDeserializeTests.ExtraMembers, SerdeVisitor>("ExtraMembers", fieldNames, visitor);
            }

            private sealed class SerdeVisitor : Serde.IDeserializeVisitor<Serde.Test.JsonDeserializeTests.ExtraMembers>
            {
                public string ExpectedTypeName => "Serde.Test.JsonDeserializeTests.ExtraMembers";
                Serde.Test.JsonDeserializeTests.ExtraMembers Serde.IDeserializeVisitor<Serde.Test.JsonDeserializeTests.ExtraMembers>.VisitDictionary<D>(ref D d)
                {
                    Serde.Option<int> b = default;
                    while (d.TryGetNextKey<string, StringWrap>(out string? key))
                    {
                        switch (key)
                        {
                            case "b":
                                b = d.GetNextValue<int, Int32Wrap>();
                                break;
                            default:
                                break;
                        }
                    }

                    var newType = new Serde.Test.JsonDeserializeTests.ExtraMembers()
                    {b = b.GetValueOrThrow("b"), };
                    return newType;
                }
            }
        }
    }
}