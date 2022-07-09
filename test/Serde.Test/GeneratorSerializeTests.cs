
using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static Serde.Test.GeneratorTestUtils;

namespace Serde.Test
{
    public class GeneratorSerializeTests
    {
        [Fact]
        public Task Rgb()
        {
            var src = @"
using Serde;
[GenerateSerialize]
partial struct Rgb
{
    public byte Red, Green, Blue;
}";
            return VerifySerialize(src, "Rgb", @"
#nullable enable
using Serde;

partial struct Rgb : Serde.ISerialize
{
    void Serde.ISerialize.Serialize(ISerializer serializer)
    {
        var type = serializer.SerializeType(""Rgb"", 3);
        type.SerializeField(""Red"", new ByteWrap(this.Red));
        type.SerializeField(""Green"", new ByteWrap(this.Green));
        type.SerializeField(""Blue"", new ByteWrap(this.Blue));
        type.End();
    }
}");
        }

        [Fact]
        public Task NullableRefField()
        {
            var src = @"
[Serde.GenerateSerialize]
partial struct S
{
    public string? F;
}";
            return VerifySerialize(src, "S", @"
#nullable enable
using Serde;

partial struct S : Serde.ISerialize
{
    void Serde.ISerialize.Serialize(ISerializer serializer)
    {
        var type = serializer.SerializeType(""S"", 1);
        type.SerializeField(""F"", new NullableRefWrap.SerializeImpl<string, StringWrap>(this.F));
        type.End();
    }
}");
        }

        [Fact]
        public Task TypeDoesntImplementISerialize()
        {
            var src = @"
using Serde;
[GenerateSerialize]
partial struct S1
{
    public S2 X;
}
struct S2 { }";
            return VerifySerialize(src,
                "S1",
                @"
#nullable enable
using Serde;

partial struct S1 : Serde.ISerialize
{
    void Serde.ISerialize.Serialize(ISerializer serializer)
    {
        var type = serializer.SerializeType(""S1"", 1);
        type.End();
    }
}",
                // /0/Test0.cs(6,15): error ERR_DoesntImplementInterface: The member 'S1.X's return type 'S2' doesn't implement Serde.ISerializable. Either implement the interface, or use a remote implementation.
                DiagnosticResult.CompilerError("ERR_DoesntImplementInterface").WithSpan(6, 15, 6, 16));
        }

        [Fact]
        public Task TypeNotPartial()
        {
            var src = @"
using Serde;
[GenerateSerialize]
struct S { }
[GenerateSerialize]
class C { }";
            return VerifyDiagnostics(src,
// /0/Test0.cs(4,8): error ERR_TypeNotPartial: The type 'S' has the `[GenerateSerializeAttribute]` applied, but the implementation can't be generated unless the type is marked 'partial'.
DiagnosticResult.CompilerError("ERR_TypeNotPartial").WithSpan(4, 8, 4, 9),
// /0/Test0.cs(6,7): error ERR_TypeNotPartial: The type 'C' has the `[GenerateSerializeAttribute]` applied, but the implementation can't be generated unless the type is marked 'partial'.
DiagnosticResult.CompilerError("ERR_TypeNotPartial").WithSpan(6, 7, 6, 8)
            );
        }

        [Fact]
        public Task TypeWithArray()
        {
            var src = @"
using Serde;
[GenerateSerialize]
partial class C
{
    public readonly int[] IntArr = new[] { 1, 2, 3 };
}";
            return VerifySerialize(src, "C", @"
#nullable enable
using Serde;

partial class C : Serde.ISerialize
{
    void Serde.ISerialize.Serialize(ISerializer serializer)
    {
        var type = serializer.SerializeType(""C"", 1);
        type.SerializeField(""IntArr"", new ArrayWrap.SerializeImpl<int, Int32Wrap>(this.IntArr));
        type.End();
    }
}");
        }

        [Fact]
        public Task NestedArray()
        {
            var src = @"
using Serde;
[GenerateSerialize]
partial class C
{
    public readonly int[][] NestedArr = new[] { new[] { 1 }, new[] { 2 } };
}";
            return VerifySerialize(src, "C", @"
#nullable enable
using Serde;

partial class C : Serde.ISerialize
{
    void Serde.ISerialize.Serialize(ISerializer serializer)
    {
        var type = serializer.SerializeType(""C"", 1);
        type.SerializeField(""NestedArr"", new ArrayWrap.SerializeImpl<int[], ArrayWrap.SerializeImpl<int, Int32Wrap>>(this.NestedArr));
        type.End();
    }
}");
        }

        [Fact]
        public Task NestedArray2()
        {
            var src = @"
using Serde;
[GenerateSerialize]
partial class C
{
    public readonly int[][] NestedArr = new int[][] { };
}";
            return VerifySerialize(src, "C", @"
#nullable enable
using Serde;

partial class C : Serde.ISerialize
{
    void Serde.ISerialize.Serialize(ISerializer serializer)
    {
        var type = serializer.SerializeType(""C"", 1);
        type.SerializeField(""NestedArr"", new ArrayWrap.SerializeImpl<int[], ArrayWrap.SerializeImpl<int, Int32Wrap>>(this.NestedArr));
        type.End();
    }
}");
        }

        [Fact]
        public Task ArrayOfGenerateSerialize()
        {
            var src = @"
using Serde;

partial class TestCase15
{
    [GenerateSerialize]
    public partial class Class0
    {
        public Class1[] Field0 = new Class1[]{new Class1()};
        public bool[] Field1 = new bool[]{false};
    }

    [GenerateSerialize]
    public partial class Class1
    {
        public int Field0 = int.MaxValue;
        public byte Field1 = byte.MaxValue;
    }
}";

            return VerifyGeneratedCode(src, new[] {
                ("TestCase15.Class0.ISerialize", @"
#nullable enable
using Serde;

partial class TestCase15
{
    partial class Class0 : Serde.ISerialize
    {
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            var type = serializer.SerializeType(""Class0"", 2);
            type.SerializeField(""Field0"", new ArrayWrap.SerializeImpl<TestCase15.Class1, IdWrap<TestCase15.Class1>>(this.Field0));
            type.SerializeField(""Field1"", new ArrayWrap.SerializeImpl<bool, BoolWrap>(this.Field1));
            type.End();
        }
    }
}"),
                ("TestCase15.Class1.ISerialize", @"
#nullable enable
using Serde;

partial class TestCase15
{
    partial class Class1 : Serde.ISerialize
    {
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            var type = serializer.SerializeType(""Class1"", 2);
            type.SerializeField(""Field0"", new Int32Wrap(this.Field0));
            type.SerializeField(""Field1"", new ByteWrap(this.Field1));
            type.End();
        }
    }
}")
            });
        }

        [Fact]
        public Task DictionaryGenerate()
        {
            var src = @"
using Serde;
using System.Collections.Generic;

[GenerateSerialize]
partial class C
{
    public readonly Dictionary<string, int> Map = new Dictionary<string, int>()
    {
        [""abc""] = 5,
        [""def""] = 3
    };
}
";
            return VerifySerialize(src, "C", @"
#nullable enable
using Serde;

partial class C : Serde.ISerialize
{
    void Serde.ISerialize.Serialize(ISerializer serializer)
    {
        var type = serializer.SerializeType(""C"", 1);
        type.SerializeField(""Map"", new DictWrap.SerializeImpl<string, StringWrap, int, Int32Wrap>(this.Map));
        type.End();
    }
}");
        }

        [Fact]
        public Task DictionaryGenerate2()
        {
            var src = @"
using Serde;
using System.Collections.Generic;

[GenerateSerialize]
partial record C(int X);

[GenerateSerialize]
partial class C2
{
    public readonly Dictionary<string, C> Map = new Dictionary<string, C>()
    {
        [""abc""] = new C(11),
        [""def""] = new C(3)
    };
}
";
            return VerifyGeneratedCode(src, new[] {
                ("C.ISerialize", @"
#nullable enable
using Serde;

partial record C : Serde.ISerialize
{
    void Serde.ISerialize.Serialize(ISerializer serializer)
    {
        var type = serializer.SerializeType(""C"", 1);
        type.SerializeField(""X"", new Int32Wrap(this.X));
        type.End();
    }
}"),
                ("C2.ISerialize", @"
#nullable enable
using Serde;

partial class C2 : Serde.ISerialize
{
    void Serde.ISerialize.Serialize(ISerializer serializer)
    {
        var type = serializer.SerializeType(""C2"", 1);
        type.SerializeField(""Map"", new DictWrap.SerializeImpl<string, StringWrap, C, IdWrap<C>>(this.Map));
        type.End();
    }
}")
});
        }

        [Fact]
        public Task IDictionaryImplGenerate()
        {
            var src = @"
using System;
using System.Collections;
using System.Collections.Generic;
using Serde;

record R(Dictionary<string, int> D) : IDictionary<string, int>
{
    public int this[string key] { get => ((IDictionary<string, int>)D)[key]; set => ((IDictionary<string, int>)D)[key] = value; }

    public ICollection<string> Keys => ((IDictionary<string, int>)D).Keys;

    public ICollection<int> Values => ((IDictionary<string, int>)D).Values;

    public int Count => ((ICollection<KeyValuePair<string, int>>)D).Count;

    public bool IsReadOnly => ((ICollection<KeyValuePair<string, int>>)D).IsReadOnly;

    public void Add(string key, int value)
    {
        ((IDictionary<string, int>)D).Add(key, value);
    }

    public void Add(KeyValuePair<string, int> item)
    {
        ((ICollection<KeyValuePair<string, int>>)D).Add(item);
    }

    public void Clear()
    {
        ((ICollection<KeyValuePair<string, int>>)D).Clear();
    }

    public bool Contains(KeyValuePair<string, int> item)
    {
        return ((ICollection<KeyValuePair<string, int>>)D).Contains(item);
    }

    public bool ContainsKey(string key)
    {
        return ((IDictionary<string, int>)D).ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<string, int>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<string, int>>)D).CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<string, int>>)D).GetEnumerator();
    }

    public bool Remove(string key)
    {
        return ((IDictionary<string, int>)D).Remove(key);
    }

    public bool Remove(KeyValuePair<string, int> item)
    {
        return ((ICollection<KeyValuePair<string, int>>)D).Remove(item);
    }

    public bool TryGetValue(string key, out int value)
    {
        return ((IDictionary<string, int>)D).TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)D).GetEnumerator();
    }
}
[GenerateSerialize]
partial class C
{
    public R RDictionary;
}";
            return VerifySerialize(src, "C", @"
#nullable enable
using Serde;

partial class C : Serde.ISerialize
{
    void Serde.ISerialize.Serialize(ISerializer serializer)
    {
        var type = serializer.SerializeType(""C"", 1);
        type.SerializeField(""RDictionary"", new IDictWrap.SerializeImpl<string, StringWrap, int, Int32Wrap>(this.RDictionary));
        type.End();
    }
}");
        }

        [Fact]
        public Task ExplicitWrapper()
        {
            var src = @"
using Serde;

public struct S
{
    public int X;
    public int Y;
    public S(int x, int y)
    {
        X = x;
        Y = y;
    }
}
public struct SWrap : ISerialize
{
    private readonly S _s;
    public SWrap(S s)
    {
        _s = s;
    }
    void ISerialize.Serialize(ISerializer serializer)
    {
        serializer.SerializeI32(_s.X);
        serializer.SerializeI32(_s.Y);
    }
}
[GenerateSerialize]
partial class C
{
    [SerdeWrap(typeof(SWrap))]
    public S S = new S();
}";
            return VerifySerialize(src, "C", @"
#nullable enable
using Serde;

partial class C : Serde.ISerialize
{
    void Serde.ISerialize.Serialize(ISerializer serializer)
    {
        var type = serializer.SerializeType(""C"", 1);
        type.SerializeField(""S"", new SWrap(this.S));
        type.End();
    }
}");
        }

        [Fact]
        public Task ExplicitGenericWrapper()
        {
            var src = @"
using Serde;

public struct S<T>
{
    public T Field;
    public S(T f)
    {
        Field = f;
    }
}
public struct SWrap<T, TWrap> : ISerialize, ISerializeWrap<S<T>, SWrap<T, TWrap>>
    where TWrap : struct, ISerialize, ISerializeWrap<T, TWrap>
{
    public static SWrap<T, TWrap> Create(S<T> t) => new SWrap<T, TWrap>(t);
    private readonly S<T> _s;
    public SWrap(S<T> s)
    {
        _s = s;
    }
    void ISerialize.Serialize(ISerializer serializer)
    {
        var type = serializer.SerializeType(""S"", 1);
        type.SerializeField(""S"", TWrap.Create(_s.Field));
        type.End();
    }
}
[GenerateSerialize]
partial class C
{
    [SerdeWrap(typeof(SWrap<,>))]
    public S<int> S = new S<int>(5);
}";
            return VerifySerialize(src, "C", @"
#nullable enable
using Serde;

partial class C : Serde.ISerialize
{
    void Serde.ISerialize.Serialize(ISerializer serializer)
    {
        var type = serializer.SerializeType(""C"", 1);
        type.SerializeField(""S"", new SWrap<int, Int32Wrap>(this.S));
        type.End();
    }
}");
        }

        [Fact]
        public Task EnumMember()
        {
            var src = @"
namespace Some.Nested.Namespace;

[Serde.GenerateSerialize]
partial class C
{
    public ColorInt ColorInt;
    public ColorByte ColorByte;
    public ColorLong ColorLong;
    public ColorULong ColorULong;
}
public enum ColorInt { Red = 3, Green = 5, Blue = 7 }
public enum ColorByte : byte { Red = 3, Green = 5, Blue = 7 }
public enum ColorLong : long { Red = 3, Green = 5, Blue = 7 }
public enum ColorULong : ulong { Red = 3, Green = 5, Blue = 7 }
";

            return VerifyGeneratedCode(src, new[] {
                ("Serde.ColorIntWrap", @"
namespace Serde
{
    internal readonly partial record struct ColorIntWrap(Some.Nested.Namespace.ColorInt Value);
}"),
                ("Serde.ColorIntWrap.ISerialize", """

#nullable enable
using Serde;

namespace Serde
{
    partial record struct ColorIntWrap : Serde.ISerialize
    {
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            var name = Value switch
            {
                Some.Nested.Namespace.ColorInt.Red => "Red",
                Some.Nested.Namespace.ColorInt.Green => "Green",
                Some.Nested.Namespace.ColorInt.Blue => "Blue",
                _ => null
            };
            serializer.SerializeEnumValue("ColorInt", name, new Int32Wrap((int)Value));
        }
    }
}
"""),
                ("Serde.ColorByteWrap", @"
namespace Serde
{
    internal readonly partial record struct ColorByteWrap(Some.Nested.Namespace.ColorByte Value);
}"),
                ("Serde.ColorByteWrap.ISerialize", """

#nullable enable
using Serde;

namespace Serde
{
    partial record struct ColorByteWrap : Serde.ISerialize
    {
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            var name = Value switch
            {
                Some.Nested.Namespace.ColorByte.Red => "Red",
                Some.Nested.Namespace.ColorByte.Green => "Green",
                Some.Nested.Namespace.ColorByte.Blue => "Blue",
                _ => null
            };
            serializer.SerializeEnumValue("ColorByte", name, new ByteWrap((byte)Value));
        }
    }
}
"""),
                ("Serde.ColorLongWrap", @"
namespace Serde
{
    internal readonly partial record struct ColorLongWrap(Some.Nested.Namespace.ColorLong Value);
}"),
                ("Serde.ColorLongWrap.ISerialize", """

#nullable enable
using Serde;

namespace Serde
{
    partial record struct ColorLongWrap : Serde.ISerialize
    {
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            var name = Value switch
            {
                Some.Nested.Namespace.ColorLong.Red => "Red",
                Some.Nested.Namespace.ColorLong.Green => "Green",
                Some.Nested.Namespace.ColorLong.Blue => "Blue",
                _ => null
            };
            serializer.SerializeEnumValue("ColorLong", name, new Int64Wrap((long)Value));
        }
    }
}
"""),
                ("Serde.ColorULongWrap", @"
namespace Serde
{
    internal readonly partial record struct ColorULongWrap(Some.Nested.Namespace.ColorULong Value);
}"),
                ("Serde.ColorULongWrap.ISerialize", """

#nullable enable
using Serde;

namespace Serde
{
    partial record struct ColorULongWrap : Serde.ISerialize
    {
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            var name = Value switch
            {
                Some.Nested.Namespace.ColorULong.Red => "Red",
                Some.Nested.Namespace.ColorULong.Green => "Green",
                Some.Nested.Namespace.ColorULong.Blue => "Blue",
                _ => null
            };
            serializer.SerializeEnumValue("ColorULong", name, new UInt64Wrap((ulong)Value));
        }
    }
}
"""),
                ("Some.Nested.Namespace.C.ISerialize", """

#nullable enable
using Serde;

namespace Some.Nested.Namespace
{
    partial class C : Serde.ISerialize
    {
        void Serde.ISerialize.Serialize(ISerializer serializer)
        {
            var type = serializer.SerializeType("C", 4);
            type.SerializeField("ColorInt", new ColorIntWrap(this.ColorInt));
            type.SerializeField("ColorByte", new ColorByteWrap(this.ColorByte));
            type.SerializeField("ColorLong", new ColorLongWrap(this.ColorLong));
            type.SerializeField("ColorULong", new ColorULongWrap(this.ColorULong));
            type.End();
        }
    }
}
"""),
            });
        }

        private static Task VerifySerialize(
            string src,
            string typeName,
            string expected,
            params DiagnosticResult[] diagnostics)
            => VerifyGeneratedCode(src, new[] { (typeName + ".ISerialize", expected)}, diagnostics);
    }
}