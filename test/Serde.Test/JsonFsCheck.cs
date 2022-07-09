
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FsCheck;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Serde.Test
{
    public class JsonFsCheck
    {
        private static readonly EmitOptions s_emitOptions = new EmitOptions();

        /// <summary>
        /// Types made up of built-ins should serialize with the same output
        /// as System.Text.Json.
        /// </summary>
        [Fact]
        public async Task CheckPrimitiveEquivalentsAsync()
        {
            // Generates test cases, each of which has multiple generated classes
            var testCases = Gen.Sample(4, 100, Gen.Sized(TestTypeGenerators.GenTypeDef));
            var wrappers = new MemberDeclarationSyntax[testCases.Length];
            int wrapperIndex = 0;
            foreach (var type in testCases)
            {
                var classDecls = ToMembers((TestTypeDef)type);
                // Wrap the classes used by each test case in an outer class to
                // prevent name collision
                wrappers[wrapperIndex] = ClassDeclaration(
                    attributeLists: default,
                    modifiers: TokenList(Token(SyntaxKind.PartialKeyword)),
                    identifier: Identifier("TestCase" + wrapperIndex),
                    typeParameterList: null,
                    baseList: null,
                    constraintClauses: default,
                    members: List(classDecls)).NormalizeWhitespace();
                wrapperIndex++;
            }

            var serializeStatements = new List<string>();
            serializeStatements.Add("var options = new System.Text.Json.JsonSerializerOptions() { IncludeFields = true };");
            for (int i = 0; i < wrappers.Length; i++)
            {
                var localName = "t" + i;
                var typeName = $"TestCase{i}.Type0";
                serializeStatements.Add($"var {localName} = new {typeName}();");
                var serName = $"{localName}Ser";
                serializeStatements.Add($@"string {serName} =
Serde.Json.JsonSerializer.Serialize({localName});");
                var deName = $"{localName}De";
                serializeStatements.Add($@"var {deName} =
Serde.Json.JsonSerializer.Deserialize<{typeName}>({serName});");
                serializeStatements.Add($@"if ({localName} != {deName})
{{
    throw new Exception(""Expected: "" + {localName} + Environment.NewLine + ""Actual: "" + {deName});
}}");
                serializeStatements.Add($"var stj{i} = System.Text.Json.JsonSerializer.Serialize({localName}, options);");
                serializeStatements.Add($@"if ({serName} != stj{i})
{{
    throw new Exception(""Expected: "" + {serName} + Environment.NewLine + ""Actual: "" + stj{i});
}}");
            }

            var body = string.Join(Environment.NewLine, serializeStatements);
            var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
            var mainTree = SyntaxFactory.ParseSyntaxTree($@"
using System;
using System.Collections.Generic;

namespace Serde.Test
{{
    public static class Runner
    {{
        public static void Run()
        {{
            {body}
        }}
    }}
}}", parseOptions, path: "Driver.cs");

            var allTypes = SyntaxTree(CompilationUnit(
                externs: default,
                usings: List(new [] {
                    UsingDirective(IdentifierName("System")),
                    UsingDirective(QualifiedName(
                        QualifiedName(IdentifierName("System"), IdentifierName("Collections")),
                        IdentifierName("Generic"))),
                    UsingDirective(IdentifierName("Serde")),
                    }),
                attributeLists: default,
                List(new MemberDeclarationSyntax[] {
                    NamespaceDeclaration(
                        QualifiedName(IdentifierName("Serde"), IdentifierName("Test")),
                        externs: default,
                        usings: default,
                        members: List(wrappers))
                })).NormalizeWhitespace(),
                parseOptions,
                path: "AllTypes.cs");

            var refs = new[] {
                MetadataReference.CreateFromFile(typeof(Serde.ISerialize).Assembly.Location),
            };

            var comp = CSharpCompilation.Create(
               Guid.NewGuid().ToString("N"),
               syntaxTrees: new[] { mainTree, allTypes, SyntaxFactory.ParseSyntaxTree(DeepEquals, parseOptions, "DeepEquals.cs") },
               references: (await Config.LatestTfRefs.ResolveAsync(null, default)).Concat(refs),
               new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, generalDiagnosticOption: ReportDiagnostic.Warn));

            var driver = CSharpGeneratorDriver.Create(new[] { new Generator() }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(
                comp,
                out var newComp,
                out var diagnostics);

            Assert.True(diagnostics.Length == 0, string.Join(Environment.NewLine, diagnostics));

            var peStream = new MemoryStream();
            var result = newComp.Emit(peStream,
                pdbStream: null,
                xmlDocumentationStream: null,
                win32Resources: null,
                manifestResources: null,
                options: s_emitOptions,
                cancellationToken: default);

            Assert.True(result.Success,
                string.Join(Environment.NewLine, result.Diagnostics));
            var loaded = Assembly.Load(peStream.GetBuffer());
            loaded.GetType("Serde.Test.Runner")!.GetMethod("Run")!.Invoke(null, null);
        }

        // Tuns a test type into a list of type declarations. The root type is always named
        // "Type0".
        public static IReadOnlyList<MemberDeclarationSyntax> ToMembers(TestTypeDef typeDef)
        {
            // Types have generated unique names, counting from a depth-first traversal
            var types = new List<MemberDeclarationSyntax>();
            _ = VisitType(typeDef, typeIndex: 0, types);
            // Reverse types, as they're added depth-first
            types.Reverse();
            return types;

            // Generate syntax for the given typedef, and the type definition for the generated
            // types of any fields. Returns the type index of the last type generated.
            static int VisitType(TestType type, int typeIndex, List<MemberDeclarationSyntax> types)
            {
                switch (type)
                {
                    case TestListLike arr:
                        return VisitType(arr.ElementType, typeIndex, types);
                    case TestTypeDef typeDef:
                        var members = new List<MemberDeclarationSyntax>();
                        // nextIndex tracks the number of types we're generating. Every type
                        // will get a new index, which will be used to create a unique name
                        int nextIndex = typeIndex + 1;
                        for (int fieldIndex = 0; fieldIndex < typeDef.FieldTypes.Length; fieldIndex++)
                        {
                            var field = typeDef.FieldTypes[fieldIndex];
                            var fieldTypeName = field.TypeSyntax(nextIndex);
                            ExpressionSyntax initializer = field.Value(nextIndex);
                            SyntaxList<AttributeListSyntax> attributes = default;

                            members.Add(PropertyDeclaration(
                                attributeLists: attributes,
                                modifiers: TokenList(Token(SyntaxKind.PublicKeyword)),
                                type: fieldTypeName,
                                explicitInterfaceSpecifier: null,
                                identifier: Identifier("Field" + fieldIndex),
                                accessorList: AccessorList(List(new[] {
                                    AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration,
                                        attributeLists: default,
                                        modifiers: default,
                                        keyword: Token(SyntaxKind.GetKeyword),
                                        body: null,
                                        expressionBody: null,
                                        semicolonToken: Token(SyntaxKind.SemicolonToken)),
                                    AccessorDeclaration(
                                        SyntaxKind.SetAccessorDeclaration,
                                        attributeLists: default,
                                        modifiers: default,
                                        keyword: Token(SyntaxKind.SetKeyword),
                                        body: null,
                                        expressionBody: null,
                                        semicolonToken: Token(SyntaxKind.SemicolonToken)),
                                })),
                                expressionBody: null,
                                initializer: EqualsValueClause(initializer),
                                semicolonToken: Token(SyntaxKind.SemicolonToken)));
                            nextIndex = VisitType(field, nextIndex, types);
                        }

                        var typeName = typeDef.TypeName(typeIndex);

                        // Add custom equals
                        members.Add(SyntaxFactory.ParseMemberDeclaration(GenerateEquals(typeName, typeDef.FieldTypes))!);
                        // Add custom hash code
                        members.Add(SyntaxFactory.ParseMemberDeclaration(GenerateHashCode(typeName, typeDef.FieldTypes))!);

                        // Add the type
                        types.Add(TypeDeclaration(
                            kind: SyntaxKind.RecordDeclaration,
                            attributes: List(new[] { AttributeList(SeparatedList(new[] {
                                Attribute(IdentifierName("GenerateSerialize")),
                                Attribute(IdentifierName("GenerateDeserialize")),
                            })) }),
                            modifiers: TokenList(new[] { Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword) }),
                            keyword: Token(SyntaxKind.RecordKeyword),
                            identifier: Identifier(typeName),
                            typeParameterList: null,
                            baseList: null,
                            constraintClauses: default,
                            openBraceToken: Token(SyntaxKind.OpenBraceToken),
                            members: List(members),
                            closeBraceToken: Token(SyntaxKind.CloseBraceToken),
                            semicolonToken: default
                        ));
                        return nextIndex;

                    default:
                        return typeIndex;
                }
            }

            static string GenerateEquals(string containingName, ImmutableArray<TestType> fieldTypes)
            {
                var builder = new StringBuilder();
                for (int fieldIndex = 0; fieldIndex < fieldTypes.Length; fieldIndex++)
                {
                    var fieldName = "Field" + fieldIndex;
                    if (fieldTypes[fieldIndex] is TestListLike)
                    {
                        builder.AppendLine($"eq = eq && DeepEquals.Equals({fieldName}, other.{fieldName});");
                    }
                    else
                    {
                        builder.AppendLine($"eq = eq && ({fieldName} == other.{fieldName});");
                    }
                }

                var text = @$"
                public virtual bool Equals({containingName} other)
                {{
                    bool eq = true;
                    {builder.ToString()}
                    return eq;
                }}";

                return text;
            }

            // This generates a terribly performing, but correct hash code
            static string GenerateHashCode(string containingName, ImmutableArray<TestType> fieldTypes)
            {
                var text = "public override int GetHashCode() => 0;";
                return text;
            }
        }

        const string DeepEquals = @"
using System.Collections;

public static class DeepEquals
{
    public static new bool Equals(object? left, object? right) => (left, right) switch
    {
        (IDictionary d1, IDictionary d2) => DictEquals(d1, d2),
        (IEnumerable e1, IEnumerable e2) => SequenceEquals(e1, e2),
        _ => left?.Equals(right) == true
    };

    private static bool DictEquals(IDictionary left, IDictionary right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }
        foreach (DictionaryEntry e in left)
        {
            var itemRight = right[e.Key];
            if (itemRight is null || !Equals(e.Value, itemRight))
            {
                return false;
            }
        }
        return true;
    }

    private static bool SequenceEquals(IEnumerable left, IEnumerable right)
    {
        var leftEnum = left.GetEnumerator();
        var rightEnum = right.GetEnumerator();
        while (true)
        {
            var leftHasNext = leftEnum.MoveNext();
            var rightHasNext = rightEnum.MoveNext();

            if (leftHasNext ^ rightHasNext)
            {
                return false;
            }
            if (!leftHasNext && !rightHasNext)
            {
                return true;
            }

            var leftCurrent = leftEnum.Current;
            var rightCurrent = rightEnum.Current;
            if (!Equals(leftCurrent, rightCurrent))
            {
                return false;
            }
        }
    }
}";

        public abstract record TestType
        {
            /// <summary>
            /// Produce the syntax that represents this type, e.g. in a return type position.
            /// </summary>
            public abstract TypeSyntax TypeSyntax(int typeIndex);
            /// <summary>
            /// Produce an expression that would produce a value of this type, e.g. in an initializer position.
            /// </summar>
            public abstract ExpressionSyntax Value(int typeIndex);
        }
        public abstract record TestPrimitive : TestType
        {
            public abstract SyntaxKind SyntaxKind { get; }
            public sealed override TypeSyntax TypeSyntax(int typeIndex) => PredefinedType(Token(SyntaxKind));
            public override ExpressionSyntax Value(int typeIndex) => MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                TypeSyntax(typeIndex),
                IdentifierName("MaxValue"));

        }
        public sealed record TestByte : TestPrimitive
        {
            public sealed override SyntaxKind SyntaxKind => SyntaxKind.ByteKeyword;
        }
        public sealed record TestChar : TestPrimitive
        {
            public sealed override SyntaxKind SyntaxKind => SyntaxKind.CharKeyword;
        }
        public sealed record TestBool : TestPrimitive
        {
            public sealed override SyntaxKind SyntaxKind => SyntaxKind.BoolKeyword;
            public override ExpressionSyntax Value(int typeIndex) => LiteralExpression(SyntaxKind.FalseLiteralExpression);
        }
        public sealed record TestInt : TestPrimitive
        {
            public sealed override SyntaxKind SyntaxKind => SyntaxKind.IntKeyword;
        }
        public record TestTypeDef(ImmutableArray<TestType> FieldTypes) : TestType
        {
            public override TypeSyntax TypeSyntax(int typeIndex) => IdentifierName(TypeName(typeIndex));
            public override ExpressionSyntax Value(int typeIndex) => ObjectCreationExpression(
                TypeSyntax(typeIndex),
                argumentList: ArgumentList(),
                initializer: null);
            public string TypeName(int typeIndex) => "Type" + typeIndex;
        }
        public abstract record TestListLike(int Length, TestType ElementType) : TestType
        {
            protected ExpressionSyntax[] MakeElements(int typeIndex)
            {
                var values = new ExpressionSyntax[Length];
                for (int i = 0; i < Length; i++)
                {
                    values[i] = ElementType.Value(typeIndex);
                }
                return values;
            }
        }
        public record TestTypeArray(int Length, TestType ElementType) : TestListLike(Length, ElementType)
        {
            public override ArrayTypeSyntax TypeSyntax(int typeIndex)
                => ArrayType(
                    ElementType.TypeSyntax(typeIndex),
                    List(new[] {
                        ArrayRankSpecifier(SeparatedList(new ExpressionSyntax[] { OmittedArraySizeExpression() }))
                   }));

            public override ExpressionSyntax Value(int typeIndex)
            {
                return ArrayCreationExpression(
                    TypeSyntax(typeIndex),
                    initializer: InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,
                        SeparatedList(MakeElements(typeIndex))));
            }
        }

        public record TestTypeList(int Length, TestType ElementType) : TestListLike(Length, ElementType)
        {
            public override TypeSyntax TypeSyntax(int typeIndex)
                => GenericName(Identifier("List"), TypeArgumentList(SeparatedList(new[] { ElementType.TypeSyntax(typeIndex) })));

            public override ExpressionSyntax Value(int typeIndex)
                => ObjectCreationExpression(
                    TypeSyntax(typeIndex),
                    argumentList: ArgumentList(),
                    initializer: InitializerExpression(SyntaxKind.CollectionInitializerExpression, SeparatedList(MakeElements(typeIndex)))
                    );
        }

        // Represent a Dictionary like a list, because the key must always be a string
        public record TestTypeDictionary(int Length, TestType ElementType) : TestListLike(Length, ElementType)
        {
            public override TypeSyntax TypeSyntax(int typeIndex)
                => GenericName(Identifier("Dictionary"), TypeArgumentList(SeparatedList(new[] {
                    PredefinedType(Token(SyntaxKind.StringKeyword)),
                    ElementType.TypeSyntax(typeIndex)
                })));

            public override ExpressionSyntax Value(int typeIndex)
                {
                    var p = ParseExpression("new Dictionary<string, int>() { [\"s0\"] = int.MaxValue }");
                    var real = ObjectCreationExpression(
                        TypeSyntax(typeIndex),
                        argumentList: ArgumentList(),
                        initializer: InitializerExpression(SyntaxKind.ObjectInitializerExpression, SeparatedList(
                            GetInitializerExpressions(typeIndex)
                    )));
                    return real;
                }

            private ExpressionSyntax[] GetInitializerExpressions(int typeIndex)
            {
                var typeValues = MakeElements(typeIndex);
                var dictExprs = new ExpressionSyntax[typeValues.Length];
                for (int i = 0; i < typeValues.Length; i++)
                {
                    dictExprs[i] = AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        ImplicitElementAccess(BracketedArgumentList(
                            SeparatedList(new[] {
                                Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal($"s{i}")))
                            }))),
                        typeValues[i]);
                }
                return dictExprs;
            }
        }

        public static class TestTypeGenerators
        {
            public static Gen<TestType> GenPrimitive { get; } = Gen.OneOf(new[] {
                    Gen.Constant<TestType>(new TestChar()),
                    Gen.Constant<TestType>(new TestBool()),
                    Gen.Constant<TestType>(new TestInt())
                });

            public static Gen<TestType> GenType(int size)
            {
                if (size == 0)
                {
                    return GenPrimitive;
                }
                else
                {
                    var genAny = Gen.OneOf(new[] {
                        GenPrimitive,
                        GenTypeArray(size),
                        GenTypeList(size),
                        GenTypeDictionary(size),
                        GenTypeDef(size),
                    });
                    return genAny;
                }
            }

            public static Gen<(int Length, TestType ElementType)> GenListLike(int size)
                // generate between 0 and 2 elements
                => Gen.Choose(0, 2).Zip(GenType(size / 2), (l, t) => (l, t));

            public static Gen<TestType> GenTypeArray(int size)
                => GenListLike(size).Select(x => (TestType)new TestTypeArray(x.Length, x.ElementType));

            public static Gen<TestType> GenTypeList(int size)
                => GenListLike(size).Select(x => (TestType)new TestTypeList(x.Length, x.ElementType));

            public static Gen<TestType> GenTypeDictionary(int size)
                => GenListLike(size).Select(x => (TestType)new TestTypeDictionary(x.Length, x.ElementType));

            public static Gen<TestType> GenTypeDef(int size)
            {
                // generate class with between 1 and 3 fields
                return Gen.Choose(1, 3)
                    .SelectMany(arraySize => ImmArrayOf(arraySize, GenType(size / 2))
                        .Select(types => ((TestType)new TestTypeDef(types))));
            }

            public static Gen<ImmutableArray<T>> ImmArrayOf<T>(int n, Gen<T> gen)
                => Gen.ArrayOf(n, gen).Select(a => a.ToImmutableArray());
        }
    }
}