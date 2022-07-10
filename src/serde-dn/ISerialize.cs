using System;

namespace Serde;

public interface ISerialize
{
    void Serialize(ISerializer serializer);
}

public interface ISerializeType
{
    void SerializeField<T>(string name, T value) where T : ISerialize;
    void SerializeField<T>(string name, T value, ReadOnlySpan<Attribute> attributes) where T : ISerialize
        => SerializeField(name, value);
    void SkipField(string name) { }
    void End();
}

public interface ISerializeEnumerable
{
    void SerializeElement<T>(T value) where T : ISerialize;
    void End();
}

public interface ISerializeDictionary
{
    void SerializeKey<T>(T key) where T : ISerialize;
    void SerializeValue<T>(T value) where T : ISerialize;
    void End();
}

public interface ISerializer
{
    void SerializeBool(bool b);
    void SerializeChar(char c);
    void SerializeByte(byte b);
    void SerializeU16(ushort u16);
    void SerializeU32(uint u32);
    void SerializeU64(ulong u64);
    void SerializeSByte(sbyte b);
    void SerializeI16(short i16);
    void SerializeI32(int i32);
    void SerializeI64(long i64);
    void SerializeFloat(float f);
    void SerializeDouble(double d);
    void SerializeDecimal(decimal d);
    void SerializeString(string s);
    void SerializeNull();
    void SerializeNotNull<T>(T t) where T : notnull, ISerialize;
    void SerializeEnumValue<T>(string enumName, string? valueName, T value) where T : notnull, ISerialize;

    ISerializeType SerializeType(string name, int numFields);
    ISerializeEnumerable SerializeEnumerable(string typeName, int? length);
    ISerializeDictionary SerializeDictionary(int? length);
}