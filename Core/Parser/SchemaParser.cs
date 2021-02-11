﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Lexer;
using Core.Lexer.Extensions;
using Core.Lexer.Tokenization;
using Core.Lexer.Tokenization.Models;
using Core.Meta;
using Core.Meta.Attributes;
using Core.Meta.Extensions;
using Core.Meta.Interfaces;
using Core.Parser.Extensions;

namespace Core.Parser
{
    public class SchemaParser
    {
        private readonly SchemaLexer _lexer;
        private readonly Dictionary<string, Definition> _definitions = new Dictionary<string, Definition>();
        /// <summary>
        /// A set of references to named types found in message/struct definitions:
        /// the left token is the type name, and the right token is the definition it's used in (used to report a helpful error).
        /// </summary>
        private readonly HashSet<(Token, Token)> _typeReferences = new HashSet<(Token, Token)>();
        private uint _index;
        private readonly SchemaOptions _options;
        private Token[] _tokens = new Token[] { };

        /// <summary>
        /// Creates a new schema parser instance from some schema files on disk.
        /// </summary>
        /// <param name="schemaPaths">The Bebop schema files that will be parsed</param>
        /// <param name="nameSpace"></param>
        public SchemaParser(List<string> schemaPaths, SchemaOptions options)
        {
            _lexer = SchemaLexer.FromSchemaPaths(schemaPaths);
            _options = options;
        }

        /// <summary>
        /// Creates a new schema parser instance and loads the schema into memory.
        /// </summary>
        /// <param name="textualSchema">A string representation of a schema.</param>
        /// <param name="nameSpace"></param>
        public SchemaParser(string textualSchema, SchemaOptions options)
        {
            _lexer = SchemaLexer.FromTextualSchema(textualSchema);
            _options = options;
        }

        /// <summary>
        ///     Gets the <see cref="Token"/> at the current <see cref="_index"/>.
        /// </summary>
        private Token CurrentToken => _tokens[_index];

        /// <summary>
        /// Tokenize the input file, storing the result in <see cref="_tokens"/>.
        /// </summary>
        /// <returns></returns>
        private async Task Tokenize()
        {
            var collection = new List<Token>();
            await foreach (var token in _lexer.TokenStream())
            {
                collection.Add(token);
            }
            _tokens = collection.ToArray();
        }

        /// <summary>
        ///     Peeks a token at the specified <paramref name="index"/>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private Token PeekToken(uint index) => _tokens[index];

        /// <summary>
        ///     Sets the current token stream position to the specified <paramref name="index"/>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private Token Base(uint index)
        {
            _index = index;
            return _tokens[index];
        }

        /// <summary>
        ///     If the <see cref="CurrentToken"/> matches the specified <paramref name="kind"/>, advance the token stream
        ///     <see cref="_index"/> forward
        /// </summary>
        /// <param name="kind">The <see cref="TokenKind"/> to eat</param>
        /// <returns></returns>
        private bool Eat(TokenKind kind)
        {

            if (CurrentToken.Kind == kind)
            {
                _index++;
                return true;
            }
            return false;
        }

        /// <summary>
        ///     If the <see cref="CurrentToken"/> matches the specified <paramref name="kind"/>, advance the token stream
        ///     <see cref="_index"/> forward.
        ///     Otherwise throw a <see cref="UnexpectedTokenException"/>
        /// </summary>
        /// <param name="kind">The <see cref="TokenKind"/> to eat</param>
        /// <returns></returns>
        private void Expect(TokenKind kind, string? hint = null)
        {

            // don't throw on block comment tokens
            if (CurrentToken.Kind == TokenKind.BlockComment)
            {
                ConsumeBlockComments();
            }
            if (!Eat(kind))
            {
                throw new UnexpectedTokenException(kind, CurrentToken, hint);
            }
        }

        /// <summary>
        /// Consume all sequential block comments.
        /// </summary>
        /// <returns>The content of the last block comment which usually proceeds a definition.</returns>
        private string ConsumeBlockComments()
        {

            var definitionDocumentation = string.Empty;
            if (CurrentToken.Kind == TokenKind.BlockComment)
            {
                while (CurrentToken.Kind == TokenKind.BlockComment)
                {
                    definitionDocumentation = CurrentToken.Lexeme;
                    Eat(TokenKind.BlockComment);
                }
            }
            return definitionDocumentation;
        }


        /// <summary>
        ///     Evaluates a schema and parses it into a <see cref="ISchema"/> object
        /// </summary>
        /// <returns></returns>
        public async Task<ISchema> Evaluate()
        {
            await Tokenize();
            _index = 0;
            _definitions.Clear();
            _typeReferences.Clear();


            while (_index < _tokens.Length && !Eat(TokenKind.EndOfFile))
            {
                ParseDefinition();
            }
            foreach (var (typeToken, definitionToken) in _typeReferences)
            {
                if (!_definitions.ContainsKey(typeToken.Lexeme))
                {
                    throw new UnrecognizedTypeException(typeToken, definitionToken.Lexeme);
                }
            }
            return new BebopSchema(_options, _definitions);
        }

        private Definition ParseDefinition()
        {
            var definitionDocumentation = ConsumeBlockComments();

            var opcodeAttribute = EatAttribute(TokenKind.Opcode);

            var isReadOnly = Eat(TokenKind.ReadOnly);

            if (Eat(TokenKind.Union))
            {
                return ParseUnionDefinition(CurrentToken, definitionDocumentation, opcodeAttribute);
            }
            else
            {
                var kind = CurrentToken switch
                {
                    _ when Eat(TokenKind.Enum) => AggregateKind.Enum,
                    _ when Eat(TokenKind.Struct) => AggregateKind.Struct,
                    _ when Eat(TokenKind.Message) => AggregateKind.Message,
                    _ => throw new UnexpectedTokenException(TokenKind.Message, CurrentToken)
                };
                return ParseNonUnionDefinition(CurrentToken, kind, isReadOnly, definitionDocumentation, opcodeAttribute);
            }
        }

        /// <summary>
        /// Consumes all the tokens belonging to an attribute
        /// </summary>
        /// <param name="kind">The kind of attribute that will be eaten</param>
        /// <returns>An instance of the attribute.</returns>
        private BaseAttribute? EatAttribute(TokenKind kind)
        {
            if (Eat(TokenKind.OpenBracket))
            {
                Expect(kind);
                var value = string.Empty;
                var isNumber = false;
                if (Eat(TokenKind.OpenParenthesis))
                {
                    value = CurrentToken.Lexeme;
                    if (Eat(TokenKind.StringExpandable) || Eat(TokenKind.StringLiteral) || kind.IsHybridValue() && Eat(TokenKind.Number))
                    {
                        isNumber = PeekToken(_index - 1).Kind == TokenKind.Number;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(
                            TokenKind.StringExpandable | TokenKind.StringLiteral | TokenKind.Number, CurrentToken);
                    }
                    Expect(TokenKind.CloseParenthesis);
                }
                Expect(TokenKind.CloseBracket);
                return kind switch
                {
                    TokenKind.Deprecated => new DeprecatedAttribute(value),
                    TokenKind.Opcode => new OpcodeAttribute(value, isNumber),
                    _ => throw new UnexpectedTokenException(kind, CurrentToken)
                };
            }
            return null;
        }


        /// <summary>
        ///     Parses a non-union data structure and adds it to the <see cref="_definitions"/> collection
        /// </summary>
        /// <param name="definitionToken">The token that names the type to define.</param>
        /// <param name="kind">The <see cref="AggregateKind"/> the type will represents.</param>
        /// <param name="isReadOnly"></param>
        /// <param name="definitionDocumentation"></param>
        /// <param name="opcodeAttribute"></param>
        /// <returns>The parsed definition.</returns>
        private Definition ParseNonUnionDefinition(Token definitionToken,
            AggregateKind kind,
            bool isReadOnly,
            string definitionDocumentation,
            BaseAttribute? opcodeAttribute)
        {
            var fields = new List<IField>();
            var kindName = kind switch { AggregateKind.Enum => "enum", AggregateKind.Struct => "struct", _ => "message" };
            var aKindName = kind switch { AggregateKind.Enum => "an enum", AggregateKind.Struct => "a struct", _ => "a message" };

            Expect(TokenKind.Identifier, hint: $"Did you forget to specify a name for this {kindName}?");
            Expect(TokenKind.OpenBrace);
            var definitionEnd = CurrentToken.Span;
            while (!Eat(TokenKind.CloseBrace))
            {
               
                var value = 0u;

                var fieldDocumentation = ConsumeBlockComments();
                // if we've reached the end of the definition after parsing documentation we need to exit.
                if (Eat(TokenKind.CloseBrace))
                {
                    break;
                }

                var deprecatedAttribute = EatAttribute(TokenKind.Deprecated);

                if (kind == AggregateKind.Message)
                {
                    const string? indexHint = "Fields in a message must be explicitly indexed: message A { 1 -> string s; 2 -> bool b; }";
                    var indexLexeme = CurrentToken.Lexeme;
                    Expect(TokenKind.Number, hint: indexHint);
                    if (!indexLexeme.TryParseUInt(out value))
                    {
                        throw new UnexpectedTokenException(TokenKind.Number, CurrentToken, "Field index must be an unsigned integer.");
                    }
                    // Parse an arrow ("->").
                    Expect(TokenKind.Hyphen, hint: indexHint);
                    Expect(TokenKind.CloseCaret, hint: indexHint);
                }

                // Parse a type name, if this isn't an enum:
                TypeBase type = kind == AggregateKind.Enum
                    ? new ScalarType(BaseType.UInt32, definitionToken.Span, definitionToken.Lexeme)
                    : ParseType(definitionToken);

                var fieldName = CurrentToken.Lexeme;
                if (TokenizerExtensions.GetKeywords().Contains(fieldName))
                {
                    throw new ReservedIdentifierException(fieldName, CurrentToken.Span);
                }
                var fieldStart = CurrentToken.Span;

                Expect(TokenKind.Identifier);

                if (kind == AggregateKind.Enum)
                {
                    Expect(TokenKind.Eq, hint: "Every constant in an enum must have an explicit literal value.");
                    var valueLexeme = CurrentToken.Lexeme;
                    Expect(TokenKind.Number, hint: "An enum constant must have a literal unsigned integer value.");
                    if (!valueLexeme.TryParseUInt(out value))
                    {
                        throw new UnexpectedTokenException(TokenKind.Number, CurrentToken, "Enum constant must be an unsigned integer.");
                    }
                }
                

                var fieldEnd = CurrentToken.Span;
                Expect(TokenKind.Semicolon, hint: $"Elements in {aKindName} are delimited using semicolons.");
                fields.Add(new Field(fieldName, type, fieldStart.Combine(fieldEnd), deprecatedAttribute, value, fieldDocumentation));
                definitionEnd = CurrentToken.Span;
            }

            var name = definitionToken.Lexeme;
            var definitionSpan = definitionToken.Span.Combine(definitionEnd);

            Definition definition = kind switch
            {
                AggregateKind.Enum => new EnumDefinition(name, definitionSpan, definitionDocumentation, fields),
                AggregateKind.Struct => new StructDefinition(name, definitionSpan, definitionDocumentation, opcodeAttribute, fields, isReadOnly),
                AggregateKind.Message => new MessageDefinition(name, definitionSpan, definitionDocumentation, opcodeAttribute, fields),
                _ => throw new InvalidOperationException("invalid kind when making definition"),
            };

            if (isReadOnly && definition is not StructDefinition)
            {
                throw new InvalidReadOnlyException(definition);
            }
            if (opcodeAttribute != null && definition is not TopLevelDefinition)
            {
                throw new InvalidOpcodeAttributeUsageException(definition);
            }

            if (_definitions.ContainsKey(name))
            {
                throw new MultipleDefinitionsException(definition);
            }
            _definitions.Add(name, definition);
            return definition;
        }

        /// <summary>
        ///     Parses a union definition and adds it to the <see cref="_definitions"/> collection.
        /// </summary>
        /// <param name="definitionToken">The token that names the union to define.</param>
        /// <param name="definitionDocumentation">The documentation above the union definition.</param>
        /// <param name="opcodeAttribute">The opcode attribute above the union definition, if any.</param>
        /// <returns>The parsed union definition.</returns>
        private UnionDefinition ParseUnionDefinition(Token definitionToken,
            string definitionDocumentation,
            BaseAttribute? opcodeAttribute)
        {
            var name = definitionToken.Lexeme;
            var branches = new List<UnionBranch>();

            Expect(TokenKind.Identifier, hint: $"Did you forget to specify a name for this union?");
            Expect(TokenKind.OpenBrace);
            var definitionEnd = CurrentToken.Span;
            while (!Eat(TokenKind.CloseBrace))
            {
                var documentation = ConsumeBlockComments();
                // if we've reached the end of the definition after parsing documentation we need to exit.
                if (Eat(TokenKind.CloseBrace))
                {
                    break;
                }

                const string? indexHint = "Branches in a union must be explicitly indexed: union U { 1 -> struct A{}  2 -> message B{} }";
                var indexLexeme = CurrentToken.Lexeme;
                Expect(TokenKind.Number, hint: indexHint);
                if (!indexLexeme.TryParseUInt(out uint discriminator))
                {
                    throw new UnexpectedTokenException(TokenKind.Number, CurrentToken, "A union branch discriminator must be an unsigned integer.");
                }
                if (discriminator > 255)
                {
                    throw new UnexpectedTokenException(TokenKind.Number, CurrentToken, "A union branch discriminator must be between 0 and 255.");
                }
                // Parse an arrow ("->").
                Expect(TokenKind.Hyphen, hint: indexHint);
                Expect(TokenKind.CloseCaret, hint: indexHint);
                var definition = ParseDefinition();
                if (definition is not TopLevelDefinition)
                {
                    throw new InvalidUnionBranchException(definition);
                }
                Eat(TokenKind.Semicolon);
                definitionEnd = CurrentToken.Span;
                branches.Add(new UnionBranch((byte)discriminator, (TopLevelDefinition)definition, documentation));
            }
            var definitionSpan = definitionToken.Span.Combine(definitionEnd);
            var unionDefinition = new UnionDefinition(name, definitionSpan, definitionDocumentation, opcodeAttribute, branches);
            _definitions.Add(name, unionDefinition);
            return unionDefinition;
        }

        /// <summary>
        ///     Parse a type name.
        /// </summary>
        /// <param name="definitionToken">
        ///     The token acting as a representative for the definition we're in.
        ///     <para/>
        ///     This is used to track how DefinedTypes are referenced in definitions, and give useful error messages.
        /// </param>
        /// <returns>An IType object.</returns>
        private TypeBase ParseType(Token definitionToken)
        {
            TypeBase type;
            Span span = CurrentToken.Span;
            if (Eat(TokenKind.Map))
            {
                // Parse "map[k, v]".
                var mapHint = "The syntax for map types is: map[KeyType, ValueType].";
                Expect(TokenKind.OpenBracket, hint: mapHint);
                var keyType = ParseType(definitionToken);
                if (!IsValidMapKeyType(keyType))
                {
                    throw new InvalidMapKeyTypeException(keyType);
                }
                Expect(TokenKind.Comma, hint: mapHint);
                var valueType = ParseType(definitionToken);
                span = span.Combine(CurrentToken.Span);
                Expect(TokenKind.CloseBracket, hint: mapHint);
                type = new MapType(keyType, valueType, span, $"map[{keyType.AsString}, {valueType.AsString}]");
            }
            else if (Eat(TokenKind.Array))
            {
                // Parse "array[v]".
                var arrayHint = "The syntax for array types is either array[Type] or Type[].";
                Expect(TokenKind.OpenBracket, hint: arrayHint);
                var valueType = ParseType(definitionToken);
                Expect(TokenKind.CloseBracket, hint: arrayHint);
                type = new ArrayType(valueType, span, $"array[{valueType.AsString}]");
            }
            else if (CurrentToken.TryParseBaseType(out var baseType))
            {
                type = new ScalarType(baseType!.Value, span, CurrentToken.Lexeme);
                // Consume the matched basetype name as an identifier:
                Expect(TokenKind.Identifier);
            }
            else
            {
                type = new DefinedType(CurrentToken.Lexeme, span, CurrentToken.Lexeme);
                _typeReferences.Add((CurrentToken, definitionToken));
                // Consume the type name as an identifier:
                Expect(TokenKind.Identifier);
            }

            // Parse any postfix "[]".
            while (Eat(TokenKind.OpenBracket))
            {
                span = span.Combine(CurrentToken.Span);
                Expect(TokenKind.CloseBracket, hint: "The syntax for array types is T[]. You can't specify a fixed size.");
                type = new ArrayType(type, span, $"{type.AsString}[]");
            }

            return type;
        }

        private static bool IsValidMapKeyType(TypeBase keyType)
        {
            return keyType is ScalarType;
        }
    }
}
