
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace Serde.Json
{
    public sealed partial class JsonDeserializer : IDeserializer
    {
        private byte[] _utf8Bytes;
        private JsonReaderState _readerState;
        private int _offset;

        public static JsonDeserializer FromString(string s)
        {
            return new JsonDeserializer(Encoding.UTF8.GetBytes(s));
        }

        private JsonDeserializer(byte[] bytes)
        {
            _utf8Bytes = bytes;
            _readerState = default;
            _offset = 0;
        }

        private void SaveState(in Utf8JsonReader reader)
        {
            _readerState = reader.CurrentState;
            _offset += (int)reader.BytesConsumed;
        }

        private Utf8JsonReader GetReader()
        {
            return new Utf8JsonReader(_utf8Bytes.AsSpan()[_offset..], isFinalBlock: true, _readerState);
        }

        public T DeserializeAny<T, V>(V v) where V : class, IDeserializeVisitor<T>
        {
            var reader = GetReader();
            reader.ReadOrThrow();
            T result;
            switch (reader.TokenType)
            {
                case JsonTokenType.StartArray:
                    result = DeserializeEnumerable<T, V>(v);
                    break;

                case JsonTokenType.Number:
                    result = DeserializeI64<T, V>(v);
                    break;

                case JsonTokenType.StartObject:
                    result = DeserializeDictionary<T, V>(v);
                    break;

                case JsonTokenType.String:
                    result = DeserializeString<T, V>(v);
                    break;

                case JsonTokenType.True:
                case JsonTokenType.False:
                    result = DeserializeBool<T, V>(v);
                    break;

                default:
                    throw new InvalidDeserializeValueException($"Could not deserialize '{reader.TokenType}");
            }
            return result;
        }

        public T DeserializeBool<T, V>(V v) where V : class, IDeserializeVisitor<T>
        {
            var reader = GetReader();
            reader.ReadOrThrow();
            bool b = reader.GetBoolean();
            SaveState(reader);
            return v.VisitBool(b);
        }

        public T DeserializeDictionary<T, V>(V v) where V : class, IDeserializeVisitor<T>
        {
            var reader = GetReader();
            reader.ReadOrThrow();

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new InvalidDeserializeValueException("Expected object start");
            }

            SaveState(reader);
            var map = new DeDictionary(this);
            return v.VisitDictionary(ref map);
        }

        public T DeserializeFloat<T, V>(V v) where V : class, IDeserializeVisitor<T>
            => DeserializeDouble<T, V>(v);

        public T DeserializeDouble<T, V>(V v) where V : class, IDeserializeVisitor<T>
        {
            var reader = GetReader();
            var d = reader.GetDouble();
            _readerState = reader.CurrentState;
            return v.VisitDouble(d);
        }

        public T DeserializeDecimal<T, V>(V v) where V : class, IDeserializeVisitor<T>
        {
            var reader = GetReader();
            var d = reader.GetDecimal();
            _readerState = reader.CurrentState;
            return v.VisitDecimal(d);
        }

        public T DeserializeEnumerable<T, V>(V v) where V : class, IDeserializeVisitor<T>
        {
            var reader = GetReader();
            reader.ReadOrThrow();

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new InvalidDeserializeValueException("Expected array start");
            }

            SaveState(reader);
            var enumerable = new DeEnumerable(this);
            return v.VisitEnumerable(ref enumerable);
        }

        private struct DeEnumerable : IDeserializeEnumerable
        {
            private JsonDeserializer _deserializer;
            public DeEnumerable(JsonDeserializer de)
            {
                _deserializer = de;
            }
            public int? SizeOpt => null;

            public bool TryGetNext<T, D>([MaybeNullWhen(false)] out T next)
                where D : IDeserialize<T>
            {
                var reader = _deserializer.GetReader();
                // Check if the next token is the end of the array, but don't advance the stream if not
                reader.ReadOrThrow();
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    _deserializer.SaveState(reader);
                    next = default;
                    return false;
                }
                // Don't save state
                next = D.Deserialize(ref _deserializer);
                return true;
            }
        }

        private struct DeDictionary : IDeserializeDictionary
        {
            private JsonDeserializer _deserializer;
            public DeDictionary(JsonDeserializer de)
            {
                _deserializer = de;
            }

            public int? SizeOpt => null;

            public bool TryGetNextEntry<K, DK, V, DV>([MaybeNullWhen(false)] out (K, V) next)
                where DK : IDeserialize<K>
                where DV : IDeserialize<V>
            {
                // Don't save state
                if (!TryGetNextKey<K, DK>(out K? nextKey))
                {
                    next = default;
                    return false;
                }
                var nextValue = GetNextValue<V, DV>();
                next = (nextKey, nextValue);
                return true;
            }

            public bool TryGetNextKey<K, D>([MaybeNullWhen(false)] out K next) where D : IDeserialize<K>
            {
                while (true)
                {
                    var reader = _deserializer.GetReader();
                    reader.ReadOrThrow();
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.EndObject:
                            // Check if the next token is the end of the object, but don't advance the stream if not
                            _deserializer.SaveState(reader);
                            next = default;
                            return false;
                        case JsonTokenType.PropertyName:
                            next = D.Deserialize(ref _deserializer);
                            return true;
                        default:
                            // If we aren't at a property name, we must be at a value and intending to skip it
                            // Call Skip in case we are starting a new array or object. Doesn't do
                            // anything for bare tokens, but we've already read one token forward above,
                            // so we can simply save the state and continue
                            reader.Skip();
                            _deserializer.SaveState(reader);
                            break;
                    }
                }
            }

            public V GetNextValue<V, D>() where D : IDeserialize<V>
            {
                return D.Deserialize(ref _deserializer);
            }
        }

        public T DeserializeSByte<T, V>(V v) where V : class, IDeserializeVisitor<T>
            => DeserializeI64<T, V>(v);

        public T DeserializeI16<T, V>(V v) where V : class, IDeserializeVisitor<T>
            => DeserializeI64<T, V>(v);


        public T DeserializeI32<T, V>(V v) where V : class, IDeserializeVisitor<T>
            => DeserializeI64<T, V>(v);

        public T DeserializeI64<T, V>(V v) where V : class, IDeserializeVisitor<T>
        {
            var reader = GetReader();
            reader.ReadOrThrow();
            var i64 = reader.GetInt64();
            SaveState(reader);
            return v.VisitI64(i64);
        }

        public T DeserializeString<T, V>(V v) where V : class, IDeserializeVisitor<T>
        {
            var reader = GetReader();
            reader.ReadOrThrow();
            var s = reader.GetString();
            SaveState(reader);
            return s is null
                ? v.VisitNull()
                : v.VisitString(s);
        }

        public T DeserializeIdentifier<T, V>(V v) where V : class, IDeserializeVisitor<T>
            => DeserializeString<T, V>(v);

        public T DeserializeType<T, V>(string typeName, ReadOnlySpan<string> fieldNames, V v) where V : class, IDeserializeVisitor<T>
        {
            // Types are identical to dictionaries
            return DeserializeDictionary<T, V>(v);
        }

        public T DeserializeByte<T, V>(V v) where V : class, IDeserializeVisitor<T>
            => DeserializeU64<T, V>(v);

        public T DeserializeU16<T, V>(V v) where V : class, IDeserializeVisitor<T>
            => DeserializeU64<T, V>(v);

        public T DeserializeU32<T, V>(V v) where V : class, IDeserializeVisitor<T>
            => DeserializeU64<T, V>(v);

        public T DeserializeU64<T, V>(V v) where V : class, IDeserializeVisitor<T>
        {
            var reader = GetReader();
            reader.ReadOrThrow();
            var u64 = reader.GetUInt64();
            SaveState(reader);
            return v.VisitU64(u64);
        }

        public T DeserializeChar<T, V>(V v) where V : class, IDeserializeVisitor<T>
            => DeserializeString<T, V>(v);

        public T DeserializeNullableRef<T, V>(V v)
            where V : class, IDeserializeVisitor<T>
        {
            var reader = GetReader();
            reader.ReadOrThrow();
            if (reader.TokenType == JsonTokenType.Null)
            {
                return v.VisitNull();
            }
            else
            {
                var deserializer = this;
                return v.VisitNotNull(ref deserializer);
            }
        }
    }

    internal static class Utf8JsonReaderExtensions
    {
        public static void ReadOrThrow(ref this Utf8JsonReader reader)
        {
            if (!reader.Read())
            {
                throw new InvalidDeserializeValueException("Unexpected end of stream");
            }
        }
    }
}