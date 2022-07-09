
#nullable enable
using Serde;

namespace Serde.Test
{
    partial class JsonDeserializeTests
    {
        partial struct IdStruct : Serde.IDeserialize<Serde.Test.JsonDeserializeTests.IdStruct>
        {
            static Serde.Test.JsonDeserializeTests.IdStruct Serde.IDeserialize<Serde.Test.JsonDeserializeTests.IdStruct>.Deserialize<D>(ref D deserializer)
            {
                var visitor = new SerdeVisitor();
                var fieldNames = new[]{"Id"};
                return deserializer.DeserializeType<Serde.Test.JsonDeserializeTests.IdStruct, SerdeVisitor>("IdStruct", fieldNames, visitor);
            }

            private sealed class SerdeVisitor : Serde.IDeserializeVisitor<Serde.Test.JsonDeserializeTests.IdStruct>
            {
                public string ExpectedTypeName => "Serde.Test.JsonDeserializeTests.IdStruct";
                Serde.Test.JsonDeserializeTests.IdStruct Serde.IDeserializeVisitor<Serde.Test.JsonDeserializeTests.IdStruct>.VisitDictionary<D>(ref D d)
                {
                    Serde.Option<int> id = default;
                    while (d.TryGetNextKey<string, StringWrap>(out string? key))
                    {
                        switch (key)
                        {
                            case "id":
                                id = d.GetNextValue<int, Int32Wrap>();
                                break;
                            default:
                                break;
                        }
                    }

                    var newType = new Serde.Test.JsonDeserializeTests.IdStruct()
                    {Id = id.GetValueOrThrow("Id"), };
                    return newType;
                }
            }
        }
    }
}