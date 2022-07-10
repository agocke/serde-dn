
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Serde
{
    internal static class SymbolUtilities
    {
        public static ITypeSymbol GetSymbolType(ISymbol symbol) => symbol switch
        {
            IFieldSymbol f => f.Type,
            IPropertySymbol p => p.Type,
            IParameterSymbol p => p.Type,
            ILocalSymbol l => l.Type,
            _ => throw new InvalidOperationException($"Unexpected symbol {symbol}")
        };

        public static List<DataMemberSymbol> GetPublicDataMembers(ITypeSymbol type)
        {
            var format = GetTypeOptions(type).MemberFormat;
            return type.GetMembers()
                .Where(m => m is {
                    DeclaredAccessibility: Accessibility.Public,
                    Kind: SymbolKind.Field or SymbolKind.Property,
                })
                .Select(m => new DataMemberSymbol(m, GetTypeOptions(type), GetMemberOptions(m))).ToList();
        }

        public static TypeSyntax ToFqnSyntax(this INamedTypeSymbol t) => SyntaxFactory.ParseTypeName(t.ToDisplayString());

        public static MemberOptions GetMemberOptions(ISymbol member)
        {
            var options = new MemberOptions();
            foreach (var attr in member.GetAttributes())
            {
                var attrClass = attr.AttributeClass;
                if (attrClass is null)
                {
                    continue;
                }
                if (WellKnownTypes.IsWellKnownAttribute(attrClass, WellKnownAttribute.SerdeMemberOptions))
                {
                    foreach (var named in attr.NamedArguments)
                    {
                        var value = named.Value.Value!;
                        options = named switch
                        {
                            {
                                Key: nameof(MemberOptions.NullIfMissing),
                                Value: {
                                    Kind: TypedConstantKind.Primitive,
                                    Type.SpecialType: SpecialType.System_Boolean
                                }
                            } => options with { NullIfMissing = (bool)value },
                            {
                                Key: nameof(MemberOptions.Rename),
                                Value: {
                                    Kind: TypedConstantKind.Primitive,
                                    Type.SpecialType: SpecialType.System_String
                                }
                            } => options with { Rename = (string)value },
                            {
                                Key: nameof(MemberOptions.ProvideAttributes),
                                Value: {
                                    Kind: TypedConstantKind.Primitive,
                                    Type.SpecialType: SpecialType.System_Boolean
                                }
                            } => options with { ProvideAttributes = (bool)value },
                            _ => options
                        };
                    }
                    break;
                }
            }
            return options;
        }

        internal static TypeOptions GetTypeOptions(ITypeSymbol type)
        {
            var options = new TypeOptions();
            foreach (var attr in type.GetAttributes())
            {
                var attrClass = attr.AttributeClass;
                if (attrClass is null)
                {
                    continue;
                }
                if (WellKnownTypes.IsWellKnownAttribute(attrClass, WellKnownAttribute.SerdeTypeOptions))
                {
                    foreach ((string name, TypedConstant argument) in attr.NamedArguments)
                    {
                        var value = argument.Value!;
                        options = (name, argument) switch {
                            (nameof(TypeOptions.MemberFormat),
                                {
                                    Kind: TypedConstantKind.Enum,
                                    Type.Name: nameof(MemberFormat)
                                }) => options with { MemberFormat = (MemberFormat)value },
                            (nameof(TypeOptions.DenyUnknownMembers),
                                {
                                    Kind: TypedConstantKind.Primitive,
                                    Type.SpecialType: SpecialType.System_Boolean
                                }
                            ) => options with { DenyUnknownMembers = (bool)value },
                            ( nameof(TypeOptions.ConstructorSignature),
                                {
                                    Kind: TypedConstantKind.Type,
                                }
                            ) => options with { ConstructorSignature = (ITypeSymbol)value },
                            _ => options
                        };
                    }
                    break;
                }
            }
            return options;
        }

    }
}