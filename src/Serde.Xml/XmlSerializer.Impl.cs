
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Serde.Xml;

partial class XmlSerializer : ISerializer, IDisposable
{
    private readonly XmlTextWriter _writer;
    private InternalState _internalState;

    private enum InternalState
    {
        Initial,
        Started
    }

    private static readonly Encoding UTF8NoBom = new UTF8Encoding();

    public XmlSerializer(Stream s)
    {
        _internalState = InternalState.Initial;
        _writer = new XmlTextWriter(s, UTF8NoBom);
        _writer.Formatting = Formatting.Indented;
    }

    void ISerializer.SerializeBool(bool b) => _writer.WriteValue(b);
    void ISerializer.SerializeByte(byte b) => _writer.WriteValue(b);
    void ISerializer.SerializeChar(char c) => _writer.WriteValue(c);
    void ISerializer.SerializeDouble(double d) => _writer.WriteValue(d);
    void ISerializer.SerializeFloat(float f) => _writer.WriteValue(f);
    void ISerializer.SerializeI16(short i16) => _writer.WriteValue(i16);
    void ISerializer.SerializeI32(int i32) => _writer.WriteValue(i32);
    void ISerializer.SerializeI64(long i64) => _writer.WriteValue(i64);
    void ISerializer.SerializeSByte(sbyte b) => _writer.WriteValue(b);
    void ISerializer.SerializeString(string s) => _writer.WriteValue(s);
    void ISerializer.SerializeU16(ushort u16) => _writer.WriteValue(u16);
    void ISerializer.SerializeU32(uint u32) => _writer.WriteValue(u32);
    void ISerializer.SerializeU64(ulong u64) => _writer.WriteValue((decimal)u64);
    void ISerializer.SerializeDecimal(decimal d) => _writer.WriteValue(d);

    void ISerializer.SerializeEnumValue<T>(string enumName, string? valueName, T value)
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

    ISerializeType ISerializer.SerializeType(string name, int numFields)
    {
        // If we're at the start of serialization, use the type name of the wrapper as the initial
        // element
        var state = _internalState;
        if (state == InternalState.Initial)
        {
            _writer.WriteStartElement(null, name, null);
            _internalState = InternalState.Started;
        }
        return new SerializeType(this, state == InternalState.Initial);
    }

    private sealed class SerializeType : ISerializeType
    {
        private readonly XmlSerializer _serializer;
        private readonly bool _writeEnd;

        public SerializeType(XmlSerializer serializer, bool writeEnd)
        {
            _serializer = serializer;
            _writeEnd = writeEnd;
        }

        public void SerializeField<T>(string name, T value) where T : ISerialize
        {
            _serializer._writer.WriteStartElement(name);
            value.Serialize(_serializer);
            _serializer._writer.WriteEndElement();
        }

        public void SerializeField<T>(string name, T value, ReadOnlySpan<Attribute> attributes) where T : ISerialize
        {
            foreach (var attr in attributes)
            {
                if (attr is XmlAttributeAttribute)
                {
                    _serializer._writer.WriteStartAttribute(name);
                    value.Serialize(_serializer);
                    _serializer._writer.WriteEndAttribute();
                    return;
                }
            }
            SerializeField(name, value);
        }

        public void End()
        {
            if (_writeEnd)
            {
                _serializer._writer.WriteEndElement();
            }
        }
    }

    ISerializeEnumerable ISerializer.SerializeEnumerable(string typeName, int? length)
    {
        _writer.WriteStartElement(typeName);
        return new SerializeEnumerable(this);
    }

    private sealed class SerializeEnumerable : ISerializeEnumerable
    {
        private readonly XmlSerializer _serializer;
        public SerializeEnumerable(XmlSerializer serializer)
        {
            _serializer = serializer;
        }

        public void SerializeElement<T>(T value) where T : ISerialize
        {
            value.Serialize(_serializer);
        }

        public void End()
        {
            _serializer._writer.WriteEndElement();
        }
    }

    ISerializeDictionary ISerializer.SerializeDictionary(int? length)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
}