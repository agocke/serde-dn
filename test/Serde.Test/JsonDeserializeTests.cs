
using System.Collections.Generic;
using Serde.Json;
using Xunit;
using static Serde.Json.JsonValue;

namespace Serde.Test
{
    public partial class JsonDeserializeTests
    {
        [Fact]
        public void DeserializeEnumerable()
        {
            var src = @"[ 1, 2 ]";
            var result = JsonSerializer.Deserialize<JsonValue>(src);
            Assert.Equal(new Array(new JsonValue[] {
                new Number(1),
                new Number(2)
            }), result);
        }

        [Fact]
        public void NestedEnumerable()
        {
            var src = @"[
                [ 1, ""abc"" ],
                [ 2 ],
                [ ""def"", 3 ],
                []
            ]";
            var result = JsonSerializer.Deserialize<JsonValue>(src);
            Assert.Equal(new Array(new JsonValue[] {
                new Array(new JsonValue[] { 1, "abc" }),
                new Array(new JsonValue[] { 2 }),
                new Array(new JsonValue[] { "def", 3 }),
                Array.Empty
            }), result);
        }

        [Fact]
        public void DeserializeObject()
        {
            var src = @"{ ""field1"": ""abc"", ""field2"": 123 }";
            var result = JsonSerializer.Deserialize<JsonValue>(src);
            Assert.Equal(new JsonValue.Object(new (string, JsonValue)[] {
                ("field1", "abc"),
                ("field2", 123)
            }), result);
        }

        [Fact]
        public void DeserializeInt()
        {
            var src = "123";
            var result = JsonSerializer.Deserialize<JsonValue>(src);
            Assert.Equal(new JsonValue.Number(123), result);
        }

        [Fact]
        public void DeserializeBool()
        {
            var src = "true";
            var result = JsonSerializer.Deserialize<JsonValue>(src);
            Assert.Equal(new JsonValue.Bool(true), result);

            src = "false";
            result = JsonSerializer.Deserialize<JsonValue>(src);
            Assert.Equal(new JsonValue.Bool(false), result);
        }

        [GenerateDeserialize]
        private partial struct ExtraMembers
        {
            public int b;
        }

        [Fact]
        public void DeserializeExtraMembers()
        {
            var src = @"
{
    ""a"" : 1,
    ""c"" : {
        ""n1"": 1,
        ""n2"": 2
    },
    ""b"" : 2,
    ""d"" : [
        { ""n1"": 1 },
        2
    ]
}";
            var result = JsonSerializer.Deserialize<ExtraMembers>(src);
            Assert.Equal(2, result.b);
        }

        [GenerateDeserialize]
        [SerdeTypeOptions(MemberFormat = MemberFormat.CamelCase)]
        private partial struct IdStruct
        {
            public int Id;
        }

        [Fact]
        public void DeserializeId()
        {
            var src = @"{
                ""_links"":{
                    ""self"":{
                        ""href"":""https://dev.azure.com/dnceng/9ee6d478-d288-47f7-aacc-f6e6d082ae6d/_apis/pipelines/686?revision=12""
                    },
                    ""web"":{
                        ""href"":""https://dev.azure.com/dnceng/9ee6d478-d288-47f7-aacc-f6e6d082ae6d/_build/definition?definitionId=686""
                    }
                },
                ""configuration"":{
                    ""path"":""eng/pipelines/runtime.yml"",
                    ""repository"":{
                        ""fullName"":""dotnet/runtime"",
                        ""connection"":{
                            ""id"":""28d5e64a-b32c-4fc1-8d1b-8d741d67ee12""
                        },
                        ""type"":""gitHub""
                    },
                    ""type"":""yaml""
                },
                ""url"":""https://dev.azure.com/dnceng/9ee6d478-d288-47f7-aacc-f6e6d082ae6d/_apis/pipelines/686?revision=12"",
                ""id"":686,
                ""revision"":12,
                ""name"":""runtime"",
                ""folder"":""\\\\dotnet\\\\runtime""
            }";
            var result = JsonSerializer.Deserialize<IdStruct>(src);
            Assert.Equal(686, result.Id);
        }

        [GenerateDeserialize]
        [SerdeTypeOptions(MemberFormat = MemberFormat.CamelCase)]
        private readonly partial record struct IdStructList
        {
            public int Count { get; init; }
            public List<IdStruct> List { get; init; }
        }

        [Fact]
        public void DeserializeNestedWithList()
        {
            var listSrc = @"
[
    {
        ""skip1"": ""x"",
        ""id"": 1531298,
        ""skip2"": false,
        ""skip3"": null
    },
    {
        ""skip1"": ""y"",
        ""id"": 32414,
        ""skip2"": false,
        ""skip3"": null
    },
    {
        ""skip1"": ""z"",
        ""id"": 14254,
        ""skip2"": false,
        ""skip3"": null
    }
]";
            var src = @$"{{
  ""count"": 3,
  ""list"": {listSrc}
}}";

            var result = JsonSerializer.Deserialize<IdStructList>(src);
            Assert.Equal(result.Count, result.List.Count);
            Assert.Equal(1531298, result.List[0].Id);
            Assert.Equal(32414, result.List[1].Id);
            Assert.Equal(14254, result.List[2].Id);
            var listDirect = JsonSerializer.DeserializeList<IdStruct>(listSrc);
            for (int i = 0; i < result.Count; i++)
            {
                Assert.Equal(result.List[i], listDirect[i]);
            }
        }

        [Fact]
        public void CheckSetToNull()
        {
            var src = @"
{
    ""Present"": ""abc"",
    ""Extra"": ""def""
}";
            var result = JsonSerializer.Deserialize<SetToNull>(src);
            Assert.Equal("abc", result.Present);
            Assert.Null(result.Missing);
        }

        [GenerateDeserialize]
        private readonly partial record struct SetToNull
        {
            public string Present { get; init; }
            [SerdeMemberOptions(NullIfMissing = true)]
            public string? Missing { get; init; }
        }

        [GenerateDeserialize]
        private partial class NullableFields
        {
            public string? S = null;
            public Dictionary<string, string?> Dict = new() {
                ["abc"] = null,
                ["def"] = "def"
            };
        }

        [Fact]
        public void NullableFieldsTest()
        {
            var src = @"
{
    ""S"": null,
    ""Dict"": {
        ""def"": ""def"",
        ""abc"": null
    }
}";
            var de = Serde.Json.JsonSerializer.Deserialize<NullableFields>(src);
            var s = new NullableFields();
            Assert.Equal(s.S, de.S);
            foreach (var (k, v) in s.Dict)
            {
                Assert.Equal(v, de.Dict[k]);
            }
            Assert.Equal(s.Dict.Count, de.Dict.Count);
        }
    }
}