
#nullable enable
using Serde;

namespace Serde.Test
{
    partial class JsonDeserializeTests
    {
        partial record struct IdStructList : Serde.IDeserialize<Serde.Test.JsonDeserializeTests.IdStructList>
        {
            static Serde.Test.JsonDeserializeTests.IdStructList Serde.IDeserialize<Serde.Test.JsonDeserializeTests.IdStructList>.Deserialize<D>(ref D deserializer)
            {
                var visitor = new SerdeVisitor();
                var fieldNames = new[]{"Count", "List"};
                return deserializer.DeserializeType<Serde.Test.JsonDeserializeTests.IdStructList, SerdeVisitor>("IdStructList", fieldNames, visitor);
            }

            private sealed class SerdeVisitor : Serde.IDeserializeVisitor<Serde.Test.JsonDeserializeTests.IdStructList>
            {
                public string ExpectedTypeName => "Serde.Test.JsonDeserializeTests.IdStructList";
                Serde.Test.JsonDeserializeTests.IdStructList Serde.IDeserializeVisitor<Serde.Test.JsonDeserializeTests.IdStructList>.VisitDictionary<D>(ref D d)
                {
                    Serde.Option<int> count = default;
                    Serde.Option<System.Collections.Generic.List<Serde.Test.JsonDeserializeTests.IdStruct>> list = default;
                    while (d.TryGetNextKey<string, StringWrap>(out string? key))
                    {
                        switch (key)
                        {
                            case "count":
                                count = d.GetNextValue<int, Int32Wrap>();
                                break;
                            case "list":
                                list = d.GetNextValue<System.Collections.Generic.List<Serde.Test.JsonDeserializeTests.IdStruct>, ListWrap.DeserializeImpl<Serde.Test.JsonDeserializeTests.IdStruct, Serde.Test.JsonDeserializeTests.IdStruct>>();
                                break;
                            default:
                                break;
                        }
                    }

                    var newType = new Serde.Test.JsonDeserializeTests.IdStructList()
                    {Count = count.GetValueOrThrow("Count"), List = list.GetValueOrThrow("List"), };
                    return newType;
                }
            }
        }
    }
}