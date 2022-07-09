
using System.Text;
using System.Xml;

namespace Serde.Xml;

partial class XmlSerializer : ISerializer
{
    private readonly XmlTextWriter _writer;

    public XmlSerializer(Stream s)
    {
        _writer = new XmlTextWriter(s, Encoding.UTF8);
    }

    void ISerializer.SerializeBool(bool b)
    {
        throw new NotImplementedException();
    }

    void ISerializer.SerializeByte(byte b)
    {
        throw new NotImplementedException();
    }

    void ISerializer.SerializeChar(char c)
    {
        throw new NotImplementedException();
    }

    ISerializeDictionary ISerializer.SerializeDictionary(int? length)
    {
        throw new NotImplementedException();
    }

    void ISerializer.SerializeDouble(double d)
    {
        throw new NotImplementedException();
    }

    ISerializeEnumerable ISerializer.SerializeEnumerable(string typeName, int? length)
    {
        throw new NotImplementedException();
    }

    void ISerializer.SerializeEnumValue<T>(string enumName, string? valueName, T value)
    {
        throw new NotImplementedException();
    }

    void ISerializer.SerializeFloat(float f)
    {
        throw new NotImplementedException();
    }

    void ISerializer.SerializeI16(short i16)
    {
        throw new NotImplementedException();
    }

    void ISerializer.SerializeI32(int i32)
    {
        throw new NotImplementedException();
    }

    void ISerializer.SerializeI64(long i64)
    {
        throw new NotImplementedException();
    }

    void ISerializer.SerializeNotNull<T>(T t)
    {
        throw new NotImplementedException();
    }

    void ISerializer.SerializeNull()
    {
        throw new NotImplementedException();
    }

    void ISerializer.SerializeSByte(sbyte b)
    {
        throw new NotImplementedException();
    }

    void ISerializer.SerializeString(string s)
    {
        throw new NotImplementedException();
    }

    ISerializeType ISerializer.SerializeType(string name, int numFields)
    {
        throw new NotImplementedException();
    }

    void ISerializer.SerializeU16(ushort u16)
    {
        throw new NotImplementedException();
    }

    void ISerializer.SerializeU32(uint u32)
    {
        throw new NotImplementedException();
    }

    void ISerializer.SerializeU64(ulong u64)
    {
        throw new NotImplementedException();
    }
}