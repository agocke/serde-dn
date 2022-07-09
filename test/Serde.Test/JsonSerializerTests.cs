using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using Serde.Json;
using Xunit;
using static Serde.Json.JsonValue;

namespace Serde.Test
{
    public partial class JsonSerializerTests
    {
        internal static string PrettyPrint(string json)
        {
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var stream = new MemoryStream();
            Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            doc.WriteTo(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private void VerifyJsonSource(JsonValue node, string expected)
        {
            var actual = Serde.Json.JsonSerializer.Serialize(node);
            using var doc = JsonDocument.Parse(actual);
            Assert.Equal(expected.Trim(), PrettyPrint(actual));
        }

        [Fact]
        public void TestNestedTypes()
        {
            var src = new Object(new (string, JsonValue)[] {
                ("field1", 1),
                ("field2", new Object(new (string, JsonValue)[] {
                    ("nested1", 5)
                })),
                ("field3", 2)
            });

            VerifyJsonSource(src, @"
{
  ""field1"": 1,
  ""field2"": {
    ""nested1"": 5
  },
  ""field3"": 2
}");
        }

        [Fact]
        public void SerializeEnumerable()
        {
            var src = new Array(ImmutableArray.Create<JsonValue>(
                1,
                2
            ));

            VerifyJsonSource(src, @"
[
  1,
  2
]");
        }

        [Fact]
        public void NestedEnumerable()
        {
            var src = new Array(ImmutableArray.Create<JsonValue>(
                1,
                new Array(ImmutableArray.Create<JsonValue>(
                    3,
                    4
                )),
                5,
                8
            ));

            VerifyJsonSource(src, @"
[
  1,
  [
    3,
    4
  ],
  5,
  8
]");
        }

        private struct JsonDictionaryWrapper : ISerialize
        {
            private readonly Dictionary<int, int> _d;
            public JsonDictionaryWrapper(Dictionary<int, int> d)
            {
                _d = d;
            }
            public void Serialize(ISerializer serializer)
            {
                var sd = serializer.SerializeDictionary(_d.Count);
                foreach (var (k,v) in _d)
                {
                    sd.SerializeKey(new StringWrap(k.ToString()));
                    sd.SerializeValue(new Int32Wrap(v));
                }
                sd.End();
            }
        }

        [Fact]
        public void TestCustomDictionary()
        {
            var d = new Dictionary<int, int>()
            {
                [3] = 5,
                [1] = 10
            };
            var js = Serde.Json.JsonSerializer.Serialize(new JsonDictionaryWrapper(d));
            var resultDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, int>>(js)!;
            Assert.Equal(d.Count, resultDict.Count);
            foreach (var (k, v) in resultDict)
            {
                Assert.Equal(d[k], v);
            }
        }

        [Fact]
        public void NullableString()
        {
            string? s = null;
            var js = Serde.Json.JsonSerializer.Serialize(new NullableRefWrap.SerializeImpl<string, StringWrap>(s));
            Assert.Equal("null", js);
            js = Serde.Json.JsonSerializer.Serialize(JsonValue.Null.Instance);
            Assert.Equal("null", js);
        }

        [GenerateSerialize]
        private partial class NullableFields
        {
            public string? S = null;
            public Dictionary<string, string?> D = new() {
                ["abc"] = null,
                ["def"] = "def"
            };
        }

        [Fact]
        public void NullableFieldsTest()
        {
            var s = new NullableFields();
            var js = Serde.Json.JsonSerializer.Serialize(s);
            var de = System.Text.Json.JsonSerializer.Deserialize<NullableFields>(js);
            Debug.Assert(de != null);
            Assert.Equal(s.S, de.S);
            foreach (var (k, v) in s.D)
            {
                Assert.Equal(v, de.D[k]);
            }
            Assert.Equal(s.D.Count, de.D.Count);
        }
    }
}