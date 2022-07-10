using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Serde.Diagnostics;
using static Serde.WellKnownTypes;

namespace Serde
{
    public partial class Generator : ISourceGenerator
    {
        private static (MethodDeclarationSyntax[], BaseListSyntax) GenerateSerializeImpl(
            GeneratorExecutionContext context,
            ITypeSymbol receiverType,
            ExpressionSyntax receiverExpr)
        {
            var statements = new List<StatementSyntax>();
            var fieldsAndProps = SymbolUtilities.GetPublicDataMembers(receiverType);

            if (receiverType.TypeKind == TypeKind.Enum)
            {
                // For enums, the generated body should look like
                // ```
                // var name = receiver switch
                // {
                //   Enum.Case1 => "Case1",
                //   Enum.Case2 => "Case2",
                //   _ => null
                // };
                // serializer.SerializeEnumValue("Enum", name, receiver);
                var enumType = ((INamedTypeSymbol)receiverType);
                var typeSyntax = enumType.ToFqnSyntax();
                var cases = fieldsAndProps.Select(m => SwitchExpressionArm(
                        ConstantPattern(QualifiedName((NameSyntax)typeSyntax, IdentifierName(m.Name))),
                        whenClause: null,
                        expression: StringLiteral(m.Name)));
                cases = cases.Concat(new[] { SwitchExpressionArm(
                    DiscardPattern(),
                    whenClause: null,
                    LiteralExpression(SyntaxKind.NullLiteralExpression, Token(SyntaxKind.NullKeyword))) });
                statements.Add(LocalDeclarationStatement(VariableDeclaration(
                    IdentifierName("var"),
                    SeparatedList(new[] { VariableDeclarator(
                        Identifier("name"),
                        argumentList: null,
                        EqualsValueClause(SwitchExpression(receiverExpr, SeparatedList(cases)))) }))));
                var wrapperName = TryGetPrimitiveWrapper(enumType.EnumUnderlyingType!, SerdeUsage.Serialize)!;
                statements.Add(ExpressionStatement(InvocationExpression(
                    QualifiedName(IdentifierName("serializer"), IdentifierName("SerializeEnumValue")),
                    ArgumentList(SeparatedList(new[] {
                        Argument(StringLiteral(receiverType.Name)),
                        Argument(IdentifierName("name")),
                        Argument(ObjectCreationExpression(
                            IdentifierName(wrapperName),
                            ArgumentList(SeparatedList(new[] { Argument(CastExpression(enumType.EnumUnderlyingType!.ToFqnSyntax(), receiverExpr)) })),
                            null))
                    }))
                )));
            }
            else
            {
                // The generated body of ISerialize is
                // `var type = serializer.SerializeType("TypeName", numFields)`
                // type.SerializeField("FieldName", receiver.FieldValue);
                // type.End();

                // `var type = serializer.SerializeType("TypeName", numFields)`
                statements.Add(LocalDeclarationStatement(VariableDeclaration(
                    IdentifierName(Identifier("var")),
                    SeparatedList(new[] {
                    VariableDeclarator(
                        Identifier("type"),
                        argumentList: null,
                        EqualsValueClause(InvocationExpression(
                            QualifiedName(IdentifierName("serializer"), IdentifierName("SerializeType")),
                            ArgumentList(SeparatedList(new [] {
                                Argument(StringLiteral(receiverType.Name)), Argument(NumericLiteral(fieldsAndProps.Count))
                            }))
                        ))
                    )
                    })
                )));

                foreach (var m in fieldsAndProps)
                {
                    var memberType = m.Type;
                    // Generate statements of the form `type.SerializeField("FieldName", FieldValue)`
                    var expr = TryMakeSerializeFieldExpr(m, memberType, context, receiverExpr);
                    if (expr is null)
                    {
                        // No built-in handling and doesn't implement ISerializable, error
                        context.ReportDiagnostic(CreateDiagnostic(
                            DiagId.ERR_DoesntImplementInterface,
                            m.Locations[0],
                            m.Symbol,
                            memberType,
                            "Serde.ISerializable"));
                    }
                    else
                    {
                        statements.Add(MakeSerializeFieldStmt(m, expr));
                    }
                }

                // `type.End();`
                statements.Add(ExpressionStatement(InvocationExpression(
                    QualifiedName(IdentifierName("type"), IdentifierName("End")),
                    ArgumentList()
                )));
            }

            // Generate method `void ISerialize.Serialize(ISerializer serializer) { ... }`
            var members = new[] { MethodDeclaration(
                attributeLists: default,
                modifiers: default,
                PredefinedType(Token(SyntaxKind.VoidKeyword)),
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier(
                    QualifiedName(IdentifierName("Serde"), IdentifierName("ISerialize"))),
                identifier: Identifier("Serialize"),
                typeParameterList: null,
                parameterList: ParameterList(SeparatedList(new[] { Parameter("ISerializer", "serializer") })),
                constraintClauses: default,
                body: Block(statements),
                expressionBody: null)
            };
            var baseList = BaseList(SeparatedList(new BaseTypeSyntax[] {
                    SimpleBaseType(QualifiedName(
                        IdentifierName("Serde"),
                        IdentifierName("ISerialize")))
                }));
            return (members, baseList);

            ExpressionSyntax? TryMakeSerializeFieldExpr(
                DataMemberSymbol m,
                ITypeSymbol memberType,
                GeneratorExecutionContext context,
                ExpressionSyntax receiverExpr)
            {
                var memberExpr = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    receiverExpr,
                    IdentifierName(m.Name));

                return MakeSerializeArgument(memberType, m, context, memberExpr);
            }

            // Make a statement like `type.SerializeField("member.Name", value)`
            static ExpressionStatementSyntax MakeSerializeFieldStmt(DataMemberSymbol member, ExpressionSyntax value)
            {
                var arguments = new List<ExpressionSyntax>() {
                        // "FieldName"
                        LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(member.GetFormattedName())),
                        // Value
                        value
                };

                // If the member is marked as providing attributes we will need to create an array of the
                // attributes and pass it as the last argument
                if (member.ProvideAttributes)
                {
                    var attributeExpressions = member.Attributes.SelectNotNull(attributeData =>
                    {
                        if (attributeData.AttributeClass is not {} attrClass)
                        {
                            return null;
                        }

                        // Construct the positional arguments to the attribute constructor
                        var args = attributeData.ConstructorArguments
                            .Select(a => Argument(ParseExpression(a.ToCSharpString()))).ToList();

                        // Construct the named arguments to the attribute constructor
                        var assignments = attributeData.NamedArguments.Select(pair =>
                            (ExpressionSyntax)AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(pair.Key),
                                ParseExpression(pair.Value.ToCSharpString()))).ToList();

                        return (ExpressionSyntax)ObjectCreationExpression(
                            attrClass.ToFqnSyntax(),
                            ArgumentList(SeparatedList(args)),
                            InitializerExpression(
                                SyntaxKind.ObjectInitializerExpression,
                                SeparatedList(assignments)));
                    }).ToList();

                    if (attributeExpressions.Count > 0)
                    {
                        arguments.Add(ArrayCreationExpression(
                            ArrayType(
                                ParseTypeName("System.Attribute"),
                                SingletonList(ArrayRankSpecifier(
                                    SingletonSeparatedList((ExpressionSyntax)OmittedArraySizeExpression())))),
                            InitializerExpression(
                                SyntaxKind.ArrayInitializerExpression,
                                SeparatedList(attributeExpressions))));
                    }
                }

                return ExpressionStatement(InvocationExpression(
                    // type.SerializeField
                    QualifiedName(IdentifierName("type"), IdentifierName("SerializeField")),
                    ArgumentList(SeparatedList(arguments.Select(Argument)))));
            }
        }

        /// <summary>
        /// Constructs the argument to a ISerializer.Serialize call, i.e. constructs a term which
        /// implements ISerialize.  SerdeDn provides wrappers for primitives and common types in the
        /// framework. If found, we generate and initialize the wrapper.
        /// </summary>
        private static ExpressionSyntax? MakeSerializeArgument(
            ITypeSymbol type,
            DataMemberSymbol member,
            GeneratorExecutionContext context,
            ExpressionSyntax memberExpr)
        {
            var argListFromMemberName = ArgumentList(SeparatedList(new[] { Argument(memberExpr) }));

            // 1. Check for an explicit wrapper

            var attr = TryGetExplicitWrapper(member);
            if (attr is { ConstructorArguments: { Length: 1 } attrArgs } &&
                attrArgs[0] is { Value: INamedTypeSymbol wrapperType })
            {
                var memberType = member.Type;
                var typeArgs = memberType switch
                {
                    INamedTypeSymbol n => n.TypeArguments,
                    _ => ImmutableArray<ITypeSymbol>.Empty
                };
                var expr = MakeWrappedExpression(wrapperType.Name, typeArgs, context, SerdeUsage.Serialize);
                if (expr is not null)
                {
                    return ObjectCreationExpression(
                        expr,
                        argListFromMemberName,
                        initializer: null);
                }
            }

            // 2. Check for a direct implementation of ISerialize

            if (ImplementsSerde(type, context, SerdeUsage.Serialize))
            {
                return memberExpr;
            }

            // 3. A wrapper for a built-in primitive (non-generic type)

            var wrapperName = TryGetPrimitiveWrapper(type, SerdeUsage.Serialize);
            if (wrapperName is not null)
            {
                return ObjectCreationExpression(
                    IdentifierName(wrapperName),
                    argListFromMemberName,
                    initializer: null);
            }

            // 4. A wrapper for a compound type (might need nested wrappers)

            TypeSyntax? wrapperTypeSyntax = TryGetCompoundWrapper(type, context, SerdeUsage.Serialize)
                // 5. Create a wrapper if appropriate
                ?? TryCreateWrapper(type, member, context, SerdeUsage.Serialize);
            if (wrapperTypeSyntax is not null)
            {
                return ObjectCreationExpression(
                    wrapperTypeSyntax,
                    argListFromMemberName,
                    initializer: null);
            }

            return null;
        }

        private static TypeSyntax? TryCreateWrapper(ITypeSymbol type, DataMemberSymbol m, GeneratorExecutionContext context, SerdeUsage usage)
        {
            if (type is ({ TypeKind: not TypeKind.Enum } and { DeclaringSyntaxReferences: { Length: > 0 } }) or
                        { CanBeReferencedByName: false } or
                        { OriginalDefinition: INamedTypeSymbol { Arity: > 0 } })
            {
                return null;
            }

            var typeName = type.Name;
            var allTypes = typeName;
            for (var parent = type.ContainingType; parent is not null; parent = parent.ContainingType)
            {
                allTypes = parent.Name + allTypes;
            }
            var wrapperName = $"{allTypes}Wrap";
            var wrapperFqn = $"Serde.{wrapperName}";
            var argName = "Value";
            var src = @$"
namespace Serde
{{
    internal readonly partial record struct {wrapperName}({type.ToDisplayString()} {argName});
}}";

            // Check if we've already created this wrapper
            if (_registry.TryAdd(context.Compilation, wrapperFqn))
            {
                context.AddSource(wrapperFqn, src);
            }
            var tree = SyntaxFactory.ParseSyntaxTree(src);
            var typeDecl = (RecordDeclarationSyntax)((NamespaceDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            GenerateImpl(usage, new TypeDeclContext(typeDecl), type, IdentifierName(argName), context);
            return IdentifierName(wrapperName);
        }

        private static AttributeData? TryGetExplicitWrapper(DataMemberSymbol member)
        {
            foreach (var attr in member.Symbol.GetAttributes())
            {
                if (attr.AttributeClass?.Name is "SerdeWrapAttribute")
                {
                    return attr;
                }
            }
            return null;
        }

        // If the target is a core type, we can wrap it
        private static string? TryGetPrimitiveWrapper(ITypeSymbol type, SerdeUsage usage)
        {
            if (type.NullableAnnotation == NullableAnnotation.Annotated)
            {
                return null;
            }
            return type.SpecialType switch
            {
                SpecialType.System_Boolean => "BoolWrap",
                SpecialType.System_Char => "CharWrap",
                SpecialType.System_Byte => "ByteWrap",
                SpecialType.System_UInt16 => "UInt16Wrap",
                SpecialType.System_UInt32 => "UInt32Wrap",
                SpecialType.System_UInt64 => "UInt64Wrap",
                SpecialType.System_SByte => "SByteWrap",
                SpecialType.System_Int16 => "Int16Wrap",
                SpecialType.System_Int32 => "Int32Wrap",
                SpecialType.System_Int64 => "Int64Wrap",
                SpecialType.System_String => "StringWrap",
                SpecialType.System_Single => "FloatWrap",
                SpecialType.System_Double => "DoubleWrap",
                SpecialType.System_Decimal => "DecimalWrap",
                _ => null
            };
        }

        /// <summary>
        /// Check to see if the type implements ISerialize or IDeserialize, depending on the WrapUsage.
        /// </summary>
        private static bool ImplementsSerde(ITypeSymbol memberType, GeneratorExecutionContext context, SerdeUsage usage)
        {
            // Nullable types are not considered as implementing the Serde interfaces -- they use wrappers to map to the underlying
            if (memberType.NullableAnnotation == NullableAnnotation.Annotated)
            {
                return false;
            }

            // Check if the type either has the GenerateSerialize attribute, or directly implements ISerialize
            // (If the type has the GenerateSerialize attribute then the generator will implement the interface)
            var attributes = memberType.GetAttributes();
            foreach (var attr in attributes)
            {
                var attrClass = attr.AttributeClass;
                if (attrClass is null)
                {
                    continue;
                }
                if (WellKnownTypes.IsWellKnownAttribute(attrClass, WellKnownAttribute.GenerateSerde))
                {
                    return true;
                }
                if (usage == SerdeUsage.Serialize &&
                    WellKnownTypes.IsWellKnownAttribute(attrClass, WellKnownAttribute.GenerateSerialize))
                {
                    return true;
                }
                if (usage == SerdeUsage.Deserialize &&
                    WellKnownTypes.IsWellKnownAttribute(attrClass, WellKnownAttribute.GenerateDeserialize))
                {
                    return true;
                }
            }

            var serdeSymbol = context.Compilation.GetTypeByMetadataName(usage == SerdeUsage.Serialize
                ? "Serde.ISerialize`4"
                : "Serde.IDeserialize`1");
            if (serdeSymbol is not null && memberType.Interfaces.Contains(serdeSymbol, SymbolEqualityComparer.Default))
            {
                return true;
            }
            return false;
        }

        private static ParameterSyntax Parameter(string typeName, string paramName, bool byRef = false) => SyntaxFactory.Parameter(
            attributeLists: default,
            modifiers: default,
            type: byRef ? SyntaxFactory.RefType(IdentifierName(typeName)) : IdentifierName(typeName),
            Identifier(paramName),
            default
        );

        private static LiteralExpressionSyntax NumericLiteral(int num)
            => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(num));

        private static LiteralExpressionSyntax StringLiteral(string text)
            => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(text));
    }
}