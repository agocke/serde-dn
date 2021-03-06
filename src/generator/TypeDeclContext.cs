
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Serde
{
    /// <summary>
    /// Provides context about a given type declaration, including what kind of typeDecl it is,
    /// and what types and namespaces it's contained in.
    /// </summary>
    internal readonly struct TypeDeclContext
    {
        public SyntaxKind Kind { get; init; }
        public string Name { get; init; }
        public List<string> NamespaceNames { get; init; }
        public List<(string Name, SyntaxKind Kind)> ParentTypeInfo { get; init; }

        public TypeDeclContext(TypeDeclarationSyntax typeDecl)
        {
            Kind = typeDecl.Kind();
            Name = typeDecl.Identifier.ValueText;
            var nsNames = new List<string>();
            var parentTypeInfos = new List<(string Name, SyntaxKind Kind)>();
            for (var parent = typeDecl.Parent; parent is not null; parent = parent.Parent)
            {
                switch (parent)
                {
                    case FileScopedNamespaceDeclarationSyntax ns:
                        nsNames.Add(ns.Name.ToString());
                        break;
                    case NamespaceDeclarationSyntax ns:
                        nsNames.Add(ns.Name.ToString());
                        break;
                    case TypeDeclarationSyntax t:
                        parentTypeInfos.Add((t.Identifier.ValueText, t.Kind()));
                        break;
                }
            }
            NamespaceNames = nsNames;
            ParentTypeInfo = parentTypeInfos;
        }

        /// <summary>
        /// Given a type, wraps it in the
        public MemberDeclarationSyntax WrapNewType(MemberDeclarationSyntax newType)
        {
            // If the original type was in a namespace or type, put this decl in the same one
            for (int i = ParentTypeInfo.Count - 1; i >= 0; i--)
            {
                var (name, kind) = ParentTypeInfo[i];
                newType = TypeDeclaration(kind, Identifier(name))
                    .WithModifiers(TokenList(Token(SyntaxKind.PartialKeyword)))
                    .WithMembers(List(new[] { newType }));
            }
            for (int i = NamespaceNames.Count - 1; i >= 0; i--)
            {
                newType = NamespaceDeclaration(
                    IdentifierName(NamespaceNames[i]),
                    externs: default,
                    usings: default,
                    members: List(new[] { newType }));
            }

            return newType;
        }
    }
}