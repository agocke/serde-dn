
#nullable enable
using Serde;

namespace Serde
{
    partial record struct AllInOneColorEnumWrap : Serde.ISerialize
    {
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            var name = Value switch
            {
                Serde.Test.AllInOne.ColorEnum.Red => "Red",
                Serde.Test.AllInOne.ColorEnum.Blue => "Blue",
                Serde.Test.AllInOne.ColorEnum.Green => "Green",
                _ => null
            };
            serializer.SerializeEnumValue("ColorEnum", name, new Int32Wrap((int)Value));
        }
    }
}