
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Serde
{
    /// <summary>
    /// Represents a member with no arguments, namely a field or property
    /// </summary>
    internal readonly struct DataMemberSymbol
    {
        private readonly ISymbol _symbol;
        private readonly TypeOptions _typeOptions;
        private readonly MemberOptions _memberOptions;

        public DataMemberSymbol(ISymbol symbol, TypeOptions typeOptions, MemberOptions memberOptions)
        {
            Debug.Assert(symbol is
                IFieldSymbol or
                IPropertySymbol { Parameters.Length: 0 });
            _symbol = symbol;
            _typeOptions = typeOptions;
            _memberOptions = memberOptions;
        }

        public ISymbol Symbol => _symbol;

        public ITypeSymbol Type => _symbol switch
        {
            IFieldSymbol f => f.Type,
            IPropertySymbol p => p.Type,
            _ => throw ExceptionUtilities.Unreachable
        };

        public NullableAnnotation NullableAnnotation => _symbol switch
        {
            IFieldSymbol f => f.NullableAnnotation,
            IPropertySymbol p => p.NullableAnnotation,
            _ => throw ExceptionUtilities.Unreachable
        };

        public ImmutableArray<Location> Locations => _symbol.Locations;

        public string Name => _symbol.Name;

        public bool NullIfMissing => _memberOptions.NullIfMissing;

        public bool ProvideAttributes => _memberOptions.ProvideAttributes;

        public ImmutableArray<AttributeData> Attributes => _symbol.GetAttributes();

        /// <summary>
        /// Retrieves the name of the member after formatting options are applied.
        /// </summary>
        public string GetFormattedName()
        {
            if (_memberOptions.Rename is { } renamed)
            {
                return renamed;
            }
            if (_typeOptions.MemberFormat == MemberFormat.None)
            {
                return Name;
            }
            var parts = ParseMemberName(Name);
            var builder = new StringBuilder();
            switch (_typeOptions.MemberFormat)
            {
                case MemberFormat.CamelCase:
                    {
                        bool first = true;
                        foreach (var part in parts)
                        {
                            if (first)
                            {
                                builder.Append(char.ToLowerInvariant(part[0]));
                                first = false;
                            }
                            else
                            {
                                builder.Append(char.ToUpperInvariant(part[0]));
                            }
                            builder.Append(part.Substring(1).ToLowerInvariant());
                        }
                    }
                    break;
                case MemberFormat.PascalCase:
                    {
                        foreach (var part in parts)
                        {
                            builder.Append(char.ToUpperInvariant(part[0]));
                            builder.Append(part.Substring(1).ToLowerInvariant());
                        }
                    }
                    break;

                default:
                    return Name;
            }
            return builder.ToString();
        }

        private static ImmutableArray<string> ParseMemberName(string name)
        {
            var resultBuilder = ImmutableArray.CreateBuilder<string>();
            var wordBuilder = new StringBuilder();
            bool wasLowercase = false;
            foreach (var c in name)
            {
                if (c == '_')
                {
                    AddWordAndClear();
                    continue;
                }
                if (char.IsUpper(c))
                {
                    if (wasLowercase)
                    {
                        AddWordAndClear();
                    }
                }
                else
                {
                    wasLowercase = true;
                }
                wordBuilder.Append(c);
            }
            AddWordAndClear();
            return resultBuilder.ToImmutable();

            void AddWordAndClear()
            {
                if (wordBuilder.Length > 0)
                {
                    resultBuilder.Add(wordBuilder.ToString());
                    wordBuilder.Clear();
                }
            }
        }

    }
}
