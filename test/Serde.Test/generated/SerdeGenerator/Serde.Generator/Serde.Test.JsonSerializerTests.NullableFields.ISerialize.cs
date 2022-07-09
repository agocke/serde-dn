
#nullable enable
using Serde;

namespace Serde.Test
{
    partial class JsonSerializerTests
    {
        partial class NullableFields : Serde.ISerialize
        {
            void Serde.ISerialize.Serialize(ISerializer serializer)
            {
                var type = serializer.SerializeType("NullableFields", 2);
                type.SerializeField("S", new NullableRefWrap.SerializeImpl<string, StringWrap>(this.S));
                type.SerializeField("D", new DictWrap.SerializeImpl<string, StringWrap, string?, NullableRefWrap.SerializeImpl<string, StringWrap>>(this.D));
                type.End();
            }
        }
    }
}