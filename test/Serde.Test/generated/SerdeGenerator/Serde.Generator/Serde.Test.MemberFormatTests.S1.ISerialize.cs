
#nullable enable
using Serde;

namespace Serde.Test
{
    partial class MemberFormatTests
    {
        partial struct S1 : Serde.ISerialize
        {
            void Serde.ISerialize.Serialize(ISerializer serializer)
            {
                var type = serializer.SerializeType("S1", 2);
                type.SerializeField("one", new Int32Wrap(this.One));
                type.SerializeField("twoWord", new Int32Wrap(this.TwoWord));
                type.End();
            }
        }
    }
}