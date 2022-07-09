
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