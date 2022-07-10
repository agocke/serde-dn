// Contains implementations of data interfaces for core types

using System;
using System.Diagnostics;

namespace Serde
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    [Conditional("EMIT_GENERATE_SERDE_ATTRIBUTE")]
    public sealed class SerdeWrapAttribute : Attribute
    {
        public SerdeWrapAttribute(Type wrapper)
        {
            Wrapper = wrapper;
        }
        public Type Wrapper { get; }
    }

    public interface ISerializeWrap<T, TWrap> where TWrap : ISerialize
    {
        abstract static TWrap Create(T t); // Should be abstract static
    }

    public readonly partial record struct BoolWrap(bool Value)
        : ISerializeWrap<bool, BoolWrap>, ISerialize, IDeserialize<bool>
    {
        public static BoolWrap Create(bool t) => new BoolWrap(t);
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            serializer.SerializeBool(Value);
        }
        static bool Serde.IDeserialize<bool>.Deserialize<D>(ref D deserializer)
        {
            var visitor = new SerdeVisitor();
            return deserializer.DeserializeBool<bool, SerdeVisitor>(visitor);
        }

        private sealed class SerdeVisitor : Serde.IDeserializeVisitor<bool>
        {
            public string ExpectedTypeName => "bool";
            bool IDeserializeVisitor<bool>.VisitBool(bool x) => x;
        }
    }

    public readonly partial record struct CharWrap(char Value)
        : ISerializeWrap<char, CharWrap>, ISerialize, IDeserialize<char>
    {
        public static CharWrap Create(char c) => new CharWrap(c);
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            serializer.SerializeChar(Value);
        }
        static char Serde.IDeserialize<char>.Deserialize<D>(ref D deserializer)
        {
            var visitor = new SerdeVisitor();
            return deserializer.DeserializeChar<char, SerdeVisitor>(visitor);
        }

        private sealed class SerdeVisitor : Serde.IDeserializeVisitor<char>
        {
            public string ExpectedTypeName => "char";
            char IDeserializeVisitor<char>.VisitChar(char c) => c;
            char IDeserializeVisitor<char>.VisitString(string s)
            {
                if (s.Length == 1)
                {
                    return s[0];
                }
                throw new InvalidDeserializeValueException("Expected type " + ExpectedTypeName);
            }
        }
    }

    public readonly partial record struct ByteWrap(byte Value)
        : ISerializeWrap<byte, ByteWrap>, ISerialize, IDeserialize<byte>
    {
        public static ByteWrap Create(byte b) => new ByteWrap(b);
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            serializer.SerializeByte(Value);
        }
        static byte Serde.IDeserialize<byte>.Deserialize<D>(ref D deserializer)
        {
            var visitor = new SerdeVisitor();
            return deserializer.DeserializeByte<byte, SerdeVisitor>(visitor);
        }

        private sealed class SerdeVisitor : Serde.IDeserializeVisitor<byte>
        {
            public string ExpectedTypeName => "byte";
            byte IDeserializeVisitor<byte>.VisitByte(byte b)    => b;
            byte IDeserializeVisitor<byte>.VisitU16(ushort u16) => Convert.ToByte(u16);
            byte IDeserializeVisitor<byte>.VisitU32(uint u32)   => Convert.ToByte(u32);
            byte IDeserializeVisitor<byte>.VisitU64(ulong u64)  => Convert.ToByte(u64);
            byte IDeserializeVisitor<byte>.VisitSByte(sbyte b)  => Convert.ToByte(b);
            byte IDeserializeVisitor<byte>.VisitI16(short i16)  => Convert.ToByte(i16);
            byte IDeserializeVisitor<byte>.VisitI32(int i32)    => Convert.ToByte(i32);
            byte IDeserializeVisitor<byte>.VisitI64(long i64)   => Convert.ToByte(i64);
        }
    }

    public readonly partial record struct UInt16Wrap(ushort Value)
        : ISerializeWrap<ushort, UInt16Wrap>, ISerialize, IDeserialize<ushort>
    {
        public static UInt16Wrap Create(ushort i) => new UInt16Wrap(i);
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            serializer.SerializeU16(Value);
        }
        static ushort Serde.IDeserialize<ushort>.Deserialize<D>(ref D deserializer)
        {
            var visitor = new SerdeVisitor();
            return deserializer.DeserializeU16<ushort, SerdeVisitor>(visitor);
        }

        private sealed class SerdeVisitor : Serde.IDeserializeVisitor<ushort>
        {
            public string ExpectedTypeName => "ushort";
            ushort IDeserializeVisitor<ushort>.VisitByte(byte b)    => b;
            ushort IDeserializeVisitor<ushort>.VisitU16(ushort u16) => u16;
            ushort IDeserializeVisitor<ushort>.VisitU32(uint u32)   => Convert.ToUInt16(u32);
            ushort IDeserializeVisitor<ushort>.VisitU64(ulong u64)  => Convert.ToUInt16(u64);
            ushort IDeserializeVisitor<ushort>.VisitSByte(sbyte b)  => Convert.ToUInt16(b);
            ushort IDeserializeVisitor<ushort>.VisitI16(short i16)  => Convert.ToUInt16(i16);
            ushort IDeserializeVisitor<ushort>.VisitI32(int i32)    => Convert.ToUInt16(i32);
            ushort IDeserializeVisitor<ushort>.VisitI64(long i64)   => Convert.ToUInt16(i64);
        }
    }

    public readonly partial record struct UInt32Wrap(uint Value)
        : ISerializeWrap<uint, UInt32Wrap>, ISerialize, IDeserialize<uint>
    {
        public static UInt32Wrap Create(uint i) => new UInt32Wrap(i);
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            serializer.SerializeU32(Value);
        }
        static uint Serde.IDeserialize<uint>.Deserialize<D>(ref D deserializer)
        {
            var visitor = new SerdeVisitor();
            return deserializer.DeserializeU32<uint, SerdeVisitor>(visitor);
        }

        private sealed class SerdeVisitor : Serde.IDeserializeVisitor<uint>
        {
            public string ExpectedTypeName => "uint";
            uint IDeserializeVisitor<uint>.VisitByte(byte b)    => b;
            uint IDeserializeVisitor<uint>.VisitU16(ushort u16) => u16;
            uint IDeserializeVisitor<uint>.VisitU32(uint u32)   => u32;
            uint IDeserializeVisitor<uint>.VisitU64(ulong u64)  => Convert.ToUInt32(u64);
            uint IDeserializeVisitor<uint>.VisitSByte(sbyte b)  => Convert.ToUInt32(b);
            uint IDeserializeVisitor<uint>.VisitI16(short i16)  => Convert.ToUInt32(i16);
            uint IDeserializeVisitor<uint>.VisitI32(int i32)    => Convert.ToUInt32(i32);
            uint IDeserializeVisitor<uint>.VisitI64(long i64)   => Convert.ToUInt32(i64);
        }
    }

    public readonly partial record struct UInt64Wrap(ulong Value)
        : ISerializeWrap<ulong, UInt64Wrap>, ISerialize, IDeserialize<ulong>
    {
        public static UInt64Wrap Create(ulong i) => new UInt64Wrap(i);
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            serializer.SerializeU64(Value);
        }
        static ulong Serde.IDeserialize<ulong>.Deserialize<D>(ref D deserializer)
        {
            var visitor = new SerdeVisitor();
            return deserializer.DeserializeU64<ulong, SerdeVisitor>(visitor);
        }

        private sealed class SerdeVisitor : Serde.IDeserializeVisitor<ulong>
        {
            public string ExpectedTypeName => "ulong";
            ulong IDeserializeVisitor<ulong>.VisitByte(byte b)    => b;
            ulong IDeserializeVisitor<ulong>.VisitU16(ushort u16) => u16;
            ulong IDeserializeVisitor<ulong>.VisitU32(uint u32)   => u32;
            ulong IDeserializeVisitor<ulong>.VisitU64(ulong u64)  => u64;
            ulong IDeserializeVisitor<ulong>.VisitSByte(sbyte b)  => Convert.ToUInt64(b);
            ulong IDeserializeVisitor<ulong>.VisitI16(short i16)  => Convert.ToUInt64(i16);
            ulong IDeserializeVisitor<ulong>.VisitI32(int i32)    => Convert.ToUInt64(i32);
            ulong IDeserializeVisitor<ulong>.VisitI64(long i64)   => Convert.ToUInt64(i64);
        }
    }

    public readonly partial record struct SByteWrap(sbyte Value)
        : ISerializeWrap<sbyte, SByteWrap>, ISerialize, IDeserialize<sbyte>
    {
        public static SByteWrap Create(sbyte i) => new SByteWrap(i);
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            serializer.SerializeSByte(Value);
        }
        static sbyte Serde.IDeserialize<sbyte>.Deserialize<D>(ref D deserializer)
        {
            var visitor = new SerdeVisitor();
            return deserializer.DeserializeSByte<sbyte, SerdeVisitor>(visitor);
        }

        private sealed class SerdeVisitor : Serde.IDeserializeVisitor<sbyte>
        {
            public string ExpectedTypeName => "sbyte";
            sbyte IDeserializeVisitor<sbyte>.VisitByte(byte b)    => Convert.ToSByte(b);
            sbyte IDeserializeVisitor<sbyte>.VisitU16(ushort u16) => Convert.ToSByte(u16);
            sbyte IDeserializeVisitor<sbyte>.VisitU32(uint u32)   => Convert.ToSByte(u32);
            sbyte IDeserializeVisitor<sbyte>.VisitU64(ulong u64)  => Convert.ToSByte(u64);
            sbyte IDeserializeVisitor<sbyte>.VisitSByte(sbyte b)  => b;
            sbyte IDeserializeVisitor<sbyte>.VisitI16(short i16)  => Convert.ToSByte(i16);
            sbyte IDeserializeVisitor<sbyte>.VisitI32(int i32)    => Convert.ToSByte(i32);
            sbyte IDeserializeVisitor<sbyte>.VisitI64(long i64)   => Convert.ToSByte(i64);
        }
    }

    public readonly partial record struct Int16Wrap(short Value)
        : ISerializeWrap<short, Int16Wrap>, ISerialize, IDeserialize<short>
    {
        public static Int16Wrap Create(short i) => new Int16Wrap(i);
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            serializer.SerializeI16(Value);
        }
        static short Serde.IDeserialize<short>.Deserialize<D>(ref D deserializer)
        {
            var visitor = new SerdeVisitor();
            return deserializer.DeserializeI16<short, SerdeVisitor>(visitor);
        }

        private sealed class SerdeVisitor : Serde.IDeserializeVisitor<short>
        {
            public string ExpectedTypeName => "short";
            short IDeserializeVisitor<short>.VisitByte(byte b)    => b;
            short IDeserializeVisitor<short>.VisitU16(ushort u16) => Convert.ToInt16(u16);
            short IDeserializeVisitor<short>.VisitU32(uint u32)   => Convert.ToInt16(u32);
            short IDeserializeVisitor<short>.VisitU64(ulong u64)  => Convert.ToInt16(u64);
            short IDeserializeVisitor<short>.VisitSByte(sbyte b)  => b;
            short IDeserializeVisitor<short>.VisitI16(short i16)  => i16;
            short IDeserializeVisitor<short>.VisitI32(int i32)    => Convert.ToInt16(i32);
            short IDeserializeVisitor<short>.VisitI64(long i64)   => Convert.ToInt16(i64);
        }
    }

    public readonly partial record struct Int32Wrap(int Value)
        : ISerializeWrap<int, Int32Wrap>, ISerialize, IDeserialize<int>
    {
        public static Int32Wrap Create(int i) => new Int32Wrap(i);
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            serializer.SerializeI32(Value);
        }
        static int Serde.IDeserialize<int>.Deserialize<D>(ref D deserializer)
        {
            var visitor = new SerdeVisitor();
            return deserializer.DeserializeI32<int, SerdeVisitor>(visitor);
        }

        private sealed class SerdeVisitor : Serde.IDeserializeVisitor<int>
        {
            public string ExpectedTypeName => "int";
            int IDeserializeVisitor<int>.VisitByte(byte b)    => b;
            int IDeserializeVisitor<int>.VisitU16(ushort u16) => u16;
            int IDeserializeVisitor<int>.VisitU32(uint u32)   => Convert.ToInt32(u32);
            int IDeserializeVisitor<int>.VisitU64(ulong u64)  => Convert.ToInt32(u64);
            int IDeserializeVisitor<int>.VisitSByte(sbyte b)  => b;
            int IDeserializeVisitor<int>.VisitI16(short i16)  => i16;
            int IDeserializeVisitor<int>.VisitI32(int i32)    => i32;
            int IDeserializeVisitor<int>.VisitI64(long i64)   => Convert.ToInt32(i64);
        }
    }

    public readonly partial record struct Int64Wrap(long Value)
        : ISerializeWrap<long, Int64Wrap>, ISerialize, IDeserialize<long>
    {
        public static Int64Wrap Create(long i) => new Int64Wrap(i);
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            serializer.SerializeI64(Value);
        }
        static long Serde.IDeserialize<long>.Deserialize<D>(ref D deserializer)
        {
            var visitor = new SerdeVisitor();
            return deserializer.DeserializeI64<long, SerdeVisitor>(visitor);
        }

        private sealed class SerdeVisitor : Serde.IDeserializeVisitor<long>
        {
            public string ExpectedTypeName => "long";
            long IDeserializeVisitor<long>.VisitByte(byte b)    => b;
            long IDeserializeVisitor<long>.VisitU16(ushort u16) => u16;
            long IDeserializeVisitor<long>.VisitU32(uint u32)   => u32;
            long IDeserializeVisitor<long>.VisitU64(ulong u64)  => Convert.ToInt64(u64);
            long IDeserializeVisitor<long>.VisitSByte(sbyte b)  => b;
            long IDeserializeVisitor<long>.VisitI16(short i16)  => i16;
            long IDeserializeVisitor<long>.VisitI32(int i32)    => i32;
            long IDeserializeVisitor<long>.VisitI64(long i64)   => Convert.ToInt64(i64);
        }
    }

    public readonly partial record struct DoubleWrap(double Value)
        : ISerializeWrap<double, DoubleWrap>, ISerialize, IDeserialize<double>
    {
        public static DoubleWrap Create(double d) => new DoubleWrap(d);
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            serializer.SerializeDouble(Value);
        }
        static double Serde.IDeserialize<double>.Deserialize<D>(ref D deserializer)
        {
            var visitor = new SerdeVisitor();
            return deserializer.DeserializeDouble<double, SerdeVisitor>(visitor);
        }

        private sealed class SerdeVisitor : Serde.IDeserializeVisitor<double>
        {
            public string ExpectedTypeName => "double";
            double IDeserializeVisitor<double>.VisitByte(byte b)    => b;
            double IDeserializeVisitor<double>.VisitU16(ushort u16) => u16;
            double IDeserializeVisitor<double>.VisitU32(uint u32)   => u32;
            double IDeserializeVisitor<double>.VisitU64(ulong u64)  => Convert.ToDouble(u64);
            double IDeserializeVisitor<double>.VisitSByte(sbyte b)  => b;
            double IDeserializeVisitor<double>.VisitI16(short i16)  => i16;
            double IDeserializeVisitor<double>.VisitI32(int i32)    => i32;
            double IDeserializeVisitor<double>.VisitI64(long i64)   => Convert.ToDouble(i64);
            double IDeserializeVisitor<double>.VisitFloat(float f) => f;
            double IDeserializeVisitor<double>.VisitDouble(double d) => d;
        }
    }

    public readonly partial record struct DecimalWrap(decimal Value)
        : ISerializeWrap<decimal, DecimalWrap>, ISerialize, IDeserialize<decimal>
    {
        public static DecimalWrap Create(decimal d) => new DecimalWrap(d);
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            serializer.SerializeDecimal(Value);
        }
        static decimal Serde.IDeserialize<decimal>.Deserialize<D>(ref D deserializer)
        {
            var visitor = new SerdeVisitor();
            return deserializer.DeserializeDecimal<decimal, SerdeVisitor>(visitor);
        }

        private sealed class SerdeVisitor : Serde.IDeserializeVisitor<decimal>
        {
            public string ExpectedTypeName => "double";
            decimal IDeserializeVisitor<decimal>.VisitByte(byte b)    => b;
            decimal IDeserializeVisitor<decimal>.VisitU16(ushort u16) => u16;
            decimal IDeserializeVisitor<decimal>.VisitU32(uint u32)   => u32;
            decimal IDeserializeVisitor<decimal>.VisitU64(ulong u64)  => u64;
            decimal IDeserializeVisitor<decimal>.VisitSByte(sbyte b)  => b;
            decimal IDeserializeVisitor<decimal>.VisitI16(short i16)  => i16;
            decimal IDeserializeVisitor<decimal>.VisitI32(int i32)    => i32;
            decimal IDeserializeVisitor<decimal>.VisitI64(long i64)   => i64;
            decimal IDeserializeVisitor<decimal>.VisitFloat(float f) => Convert.ToDecimal(f);
            decimal IDeserializeVisitor<decimal>.VisitDouble(double d) => Convert.ToDecimal(d);
        }
    }
    public readonly partial record struct StringWrap(string Value)
        : ISerializeWrap<string, StringWrap>, ISerialize, IDeserialize<string>
    {
        public static StringWrap Create(string s) => new StringWrap(s);
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            serializer.SerializeString(Value);
        }

        public static string Deserialize<D>(ref D deserializer)
            where D : IDeserializer
        {
            return deserializer.DeserializeString<string, SerdeVisitor>(new SerdeVisitor());
        }

        private class SerdeVisitor : IDeserializeVisitor<string>
        {
            public string ExpectedTypeName => "string";
            public string VisitString(string s) => s;
            string IDeserializeVisitor<string>.VisitChar(char c) => c.ToString();
        }
    }

    public static class NullableRefWrap
    {
        public readonly partial record struct SerializeImpl<T, TWrap>(T? Value)
            : ISerializeWrap<T?, SerializeImpl<T, TWrap>>, ISerialize
            where T : class
            where TWrap : struct, ISerializeWrap<T, TWrap>, ISerialize
        {
            public static SerializeImpl<T, TWrap> Create(T? t) => new SerializeImpl<T, TWrap>(t);

            void ISerialize.Serialize(ISerializer serializer)
            {
                if (Value is null)
                {
                    serializer.SerializeNull();
                }
                else
                {
                    serializer.SerializeNotNull(TWrap.Create(Value));
                }
            }
        }

        public readonly partial record struct DeserializeImpl<T, TWrap>(T? Value)
            : IDeserialize<T?>
            where T : class
            where TWrap : IDeserialize<T>
        {
            public static T? Deserialize<D>(ref D deserializer) where D : IDeserializer
            {
                return deserializer.DeserializeNullableRef<T?, Visitor>(new Visitor());
            }

            private sealed class Visitor : IDeserializeVisitor<T?>
            {
                public string ExpectedTypeName => typeof(T).ToString() + "?";

                T? IDeserializeVisitor<T?>.VisitNull()
                {
                    return null;
                }

                T? IDeserializeVisitor<T?>.VisitNotNull<D>(ref D d)
                {
                    return TWrap.Deserialize(ref d);
                }
            }
        }
    }
}