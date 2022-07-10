
using System;
using System.Text;
using System.Text.Json;

namespace Serde.Json
{
    // Using a mutable struct allows for an efficient low-allocation implementation of the
    // ISerializer interface, but mutable structs are easy to misuse in C#, so hide the
    // implementation for now.
    partial class JsonSerializer
    {
        internal readonly Utf8JsonWriter _writer;
        public JsonSerializer(Utf8JsonWriter writer)
        {
            _writer = writer;
        }
    }

    // Implementations of ISerializer
    partial class JsonSerializer : ISerializer
    {
        public void SerializeBool(bool b) => _writer.WriteBooleanValue(b);

        public void SerializeChar(char c) => SerializeString(c.ToString());

        public void SerializeByte(byte b) => _writer.WriteNumberValue(b);

        public void SerializeU16(ushort u16) => _writer.WriteNumberValue(u16);

        public void SerializeU32(uint u32) => _writer.WriteNumberValue(u32);

        public void SerializeU64(ulong u64) => _writer.WriteNumberValue(u64);

        public void SerializeSByte(sbyte b) => _writer.WriteNumberValue(b);

        public void SerializeI16(short i16) => _writer.WriteNumberValue(i16);

        public void SerializeI32(int i32) => _writer.WriteNumberValue(i32);

        public void SerializeI64(long i64) => _writer.WriteNumberValue(i64);

        public void SerializeFloat(float f) => _writer.WriteNumberValue(f);

        public void SerializeDouble(double d) => _writer.WriteNumberValue(d);

        public void SerializeDecimal(decimal d) => _writer.WriteNumberValue(d);

        public void SerializeString(string s) => _writer.WriteStringValue(s);
        public void SerializeNull() => _writer.WriteNullValue();

        public void SerializeNotNull<T>(T t) where T : notnull, ISerialize
        {
            t.Serialize(this);
        }

        public void SerializeEnumValue<T>(string enumName, string? valueName, T value) where T : notnull, ISerialize
        {
            if (valueName is null)
            {
                throw new InvalidOperationException($"Cannot serialize unnamed enum value '{value}' of enum '{enumName}'");
            }
            _writer.WriteStringValue(valueName);
        }

        public ISerializeType SerializeType(string name, int numFields)
        {
            _writer.WriteStartObject();
            return this;
        }

        public ISerializeEnumerable SerializeEnumerable(string typeName, int? count)
        {
            _writer.WriteStartArray();
            return new SerializeEnumerableImpl(this);
        }

        private sealed class SerializeEnumerableImpl : ISerializeEnumerable
        {
            private readonly JsonSerializer _s;
            public SerializeEnumerableImpl(JsonSerializer s) { _s = s; }
            void ISerializeEnumerable.SerializeElement<T>(T value)
            {
                value.Serialize(_s);
            }

            void ISerializeEnumerable.End()
            {
                _s._writer.WriteEndArray();
            }
        }

        public ISerializeDictionary SerializeDictionary(int? count)
        {
            _writer.WriteStartObject();
            return this;
        }
    }

    partial class JsonSerializer : ISerializeType
    {
        void ISerializeType.SerializeField<T>(string name, T value)
        {
            _writer.WritePropertyName(name);
            value.Serialize(this);
        }

        void ISerializeType.End()
        {
            _writer.WriteEndObject();
        }
    }

    partial class JsonSerializer : ISerializeDictionary
    {
        void ISerializeDictionary.SerializeValue<T>(T value)
        {
            value.Serialize(this);
        }
        void ISerializeDictionary.End()
        {
            _writer.WriteEndObject();
        }

        void ISerializeDictionary.SerializeKey<T>(T key)
        {
            // Grab a string value. Box to prevent internal copying and losing side-effects
            ISerializer keySerializer = new KeySerializer();
            key.Serialize(keySerializer);
            _writer.WritePropertyName(((KeySerializer)keySerializer).StringResult!);
        }

        private class KeySerializer : ISerializer
        {
            public string? StringResult = null;

            public void SerializeBool(bool b) => throw new KeyNotStringException();
            public void SerializeChar(char c) => throw new KeyNotStringException();
            public void SerializeByte(byte b) => throw new KeyNotStringException();
            public void SerializeU16(ushort u16) => throw new KeyNotStringException();

            public void SerializeU32(uint u32) => throw new KeyNotStringException();

            public void SerializeU64(ulong u64) => throw new KeyNotStringException();

            public void SerializeSByte(sbyte b) => throw new KeyNotStringException();

            public void SerializeI16(short i16) => throw new KeyNotStringException();

            public void SerializeI32(int i32) => throw new KeyNotStringException();

            public void SerializeI64(long i64) => throw new KeyNotStringException();

            public void SerializeFloat(float f) => throw new KeyNotStringException();

            public void SerializeDouble(double d) => throw new KeyNotStringException();

            public void SerializeDecimal(decimal d) => throw new KeyNotStringException();

            public void SerializeString(string s)
            {
                StringResult = s;
            }

            public void SerializeEnumValue<T>(string enumName, string? valueName, T value) where T : notnull, ISerialize
                => throw new KeyNotStringException();

            public ISerializeDictionary SerializeDictionary(int? length) => throw new KeyNotStringException();

            public ISerializeEnumerable SerializeEnumerable(string typeName, int? length) => throw new KeyNotStringException();

            public ISerializeType SerializeType(string name, int numFields) => throw new KeyNotStringException();

            public void SerializeNull() => throw new KeyNotStringException();

            public void SerializeNotNull<T>(T t) where T : notnull, ISerialize => throw new KeyNotStringException();
        }
    }
}