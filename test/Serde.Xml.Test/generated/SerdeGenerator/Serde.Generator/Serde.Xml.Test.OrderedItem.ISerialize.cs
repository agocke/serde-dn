
#nullable enable
using Serde;

namespace Serde.Xml.Test
{
    partial record OrderedItem : Serde.ISerialize
    {
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            var type = serializer.SerializeType("OrderedItem", 5);
            type.SerializeField("ItemName", new StringWrap(this.ItemName));
            type.SerializeField("Description", new StringWrap(this.Description));
            type.SerializeField("UnitPrice", new DecimalWrap(this.UnitPrice));
            type.SerializeField("Quantity", new Int32Wrap(this.Quantity));
            type.SerializeField("LineTotal", new DecimalWrap(this.LineTotal));
            type.End();
        }
    }
}