using Core.Meta;
using Core.Meta.Extensions;
using Core.Meta.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Generators.Go
{
    public class GoGenerator : Generator
    {
        private record NamelessTypeInfo(TypeBase Type, string MangledName);

        private const int IndentSpeces = 1;
        private Dictionary<string, NamelessTypeInfo> _NamelessTypes = new Dictionary<string, NamelessTypeInfo>();

        public GoGenerator(ISchema schema) :
            base(schema)
        {
        }

        public override string Compile()
        {
            FillNamelessTypes();

            // Note: Go strongly prefers the use of tabs over spaces.
            var builder = new IndentedStringBuilder(indentChar: '\t');

            if (string.IsNullOrWhiteSpace(Schema.Options.GoPkg))
            {
                throw new Exception("Go requies a package name to be specified.");
            }

            builder.AppendLine($"package {Schema.Options.GoPkg}");
            builder.AppendLine();

            builder.AppendLine("import (");
            builder.Indent(IndentSpeces);

            builder.AppendLine("bebop \"github.com/RainwayApp/bebop/Runtime/Go\"");
            builder.AppendLine("\"github.com/google/uuid\"");

            builder.Dedent(IndentSpeces);
            builder.AppendLine(")");
            builder.AppendLine();

            foreach (var definition in Schema.Definitions.Values)
            {
                WriteTypeDefinition(builder, definition);
            }

            foreach (var definition in Schema.Definitions.Values)
            {
                WriteAggregateTypeFunctions(builder, definition);
            }

            WriteNamelessTypesFunctions(builder);

            return builder.ToString();
        }

        public override void WriteAuxiliaryFiles(string outputPath)
        {
            // Nothing to do.
        }

        private void FillNamelessTypes()
        {
            foreach (var definition in Schema.Definitions.Values)
            {
                foreach (IField field in definition.Fields)
                {
                    FillNamelessTypes(field.Type);
                }
            }
        }

        private void FillNamelessTypes(TypeBase type)
        {
            switch (type)
            {
            case ArrayType at:
                AddNamelessTypeInfo(type);
                FillNamelessTypes(at.MemberType);
                break;

            case MapType mt:
                AddNamelessTypeInfo(type);
                FillNamelessTypes(mt.KeyType);
                FillNamelessTypes(mt.ValueType);
                break;
            }
        }

        private void AddNamelessTypeInfo(TypeBase type)
        {
            string typename = TypeName(type);
            if (_NamelessTypes.ContainsKey(typename))
            {
                return;
            }

            string mangledName = GenerateMangledTypeName(typename);
            _NamelessTypes.Add(typename, new NamelessTypeInfo(type, mangledName));
        }

        private string GetTypeMangledName(TypeBase type)
        {
            return _NamelessTypes[TypeName(type)].MangledName;
        }

        private static string GenerateMangledTypeName(string typename)
        {
            if (string.IsNullOrEmpty(typename))
            {
                return typename;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append('_');

            for (int i = 0; i < typename.Length;)
            {
                for (; i < typename.Length; ++i)
                {
                    char c = typename[i];
                    if (c == '_' || c == '[' || c == ']' || c == '(' || c == ')' || c == '<' || c == '>' || c == '*' || c == '.')
                    {
                        break;
                    }

                    sb.Append(c);
                }

                if (i >= typename.Length)
                {
                    break;
                }

                sb.Append('_');
                for (; i < typename.Length; ++i)
                {
                    char c = typename[i];
                    char r = c switch
                    {
                        '_' => 'U',
                        '[' => 'S',
                        ']' => 's',
                        '(' => 'R',
                        ')' => 'r',
                        '<' => 'C',
                        '>' => 'c',
                        '*' => 'P',
                        '.' => 'p',
                        _ => c,
                    };

                    if (c == r)
                    {
                        break;
                    }

                    sb.Append(r);
                }
                sb.Append('_');
            }

            return sb.ToString();
        }

        private void WriteTypeDefinition(IndentedStringBuilder builder, IDefinition definition)
        {
            switch (definition.Kind)
            {
            case AggregateKind.Enum:
                WriteEnumDefinition(builder, definition);
                break;

            case AggregateKind.Message:
            case AggregateKind.Struct:
                WriteAggregateTypeDefinition(builder, definition);
                break;
            }
        }

        /// <summary>
        /// Example output:
        ///
        ///   // An enum named AnEnum.
        ///   type AnEnum uint32
        ///   
        ///   const (
        ///     // The first value.
        ///     AnEnum_FirstValue AnEnum = 0
        ///     // The second value.
        ///     AnEnum_SecondValue AnEnum = 1
        ///     AnEnum_ThirdValue AnEnum = 5
        ///   )
        ///   
        /// </summary>
        private void WriteEnumDefinition(IndentedStringBuilder builder, IDefinition definition)
        {
            string enumName = definition.Name.ToPascalCase();

            WriteDocumentation(builder, definition.Documentation, null);
            builder.AppendLine($"type {enumName} uint32");
            builder.AppendLine();
            builder.AppendLine("const (");
            builder.Indent(IndentSpeces);

            foreach (IField field in definition.Fields)
            {
                WriteDocumentation(builder, field.Documentation, field.DeprecatedAttribute?.Value);
                builder.AppendLine($"{enumName}_{field.Name.ToPascalCase()} {enumName} = {field.ConstantValue}");
            }

            builder.Dedent(IndentSpeces);
            builder.AppendLine(")");
            builder.AppendLine();
        }

        private void WriteAggregateTypeFunctions(IndentedStringBuilder builder, IDefinition definition)
        {
            switch (definition.Kind)
            {
            case AggregateKind.Struct:
                WriteStructEncode(builder, definition);
                WriteStructDecode(builder, definition);
                break;

            case AggregateKind.Message:
                WriteMessageEncode(builder, definition);
                WriteMessageDecode(builder, definition);
                break;
            }
        }

        /// <summary>
        /// Example output:
        ///
        ///   type SomeType struct {
        ///     FirstField string
        ///     SecondField int32
        ///     ThirdField *AnotherType
        ///   }
        ///   
        /// </summary>
        private void WriteAggregateTypeDefinition(IndentedStringBuilder builder, IDefinition definition)
        {
            string structName = definition.Name.ToPascalCase();

            WriteDocumentation(builder, definition.Documentation, null);
            builder.AppendLine($"type {structName} struct {{");
            builder.Indent(IndentSpeces);

            foreach (IField field in definition.Fields)
            {
                WriteDocumentation(builder, field.Documentation, field.DeprecatedAttribute?.Value);

                string typeName = TypeName(field.Type);
                string fieldName = field.Name.ToPascalCase();

                builder.AppendLine($"{fieldName} {typeName}");
            }

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");
            builder.AppendLine();
        }

        /// <summary>
        /// Example output:
        ///
        ///   func (v *SomeClass) Encode(out []byte) []byte {
        ///     out = bebop.WriteBool(out, v.FirstField)
        ///     out = bebop.WriteFloat32(out, v.SecondField)
        ///     out = v.ThirdField.Encode(out)
        ///     return out
        ///   }
        ///   
        /// </summary>
        private void WriteStructEncode(IndentedStringBuilder builder, IDefinition definition)
        {
            builder.AppendLine($"func (v *{definition.Name.ToPascalCase()}) Encode(out []byte) []byte {{");
            builder.Indent(IndentSpeces);

            foreach (IField field in definition.Fields)
            {
                builder.AppendLine(FieldEncodeString(field.Type, $"v.{field.Name.ToPascalCase()}"));
            }

            builder.AppendLine("return out");

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");
            builder.AppendLine();
        }

        private void WriteStructDecode(IndentedStringBuilder builder, IDefinition definition)
        {
        }

        /// <summary>
        /// Example output:
        ///
        ///   func (v *SomeClass) Encode(out []byte) []byte {
        ///     lengthPlaceholder := bebop.WriteMessageLengthPlaceholder(out)
        ///     out = lengthPlaceholder
        ///     out = bebop.WriteByte(out, 1)
        ///     out = bebop.WriteBool(out, v.FirstField)
        ///     out = bebop.WriteByte(out, 2)
        ///     out = bebop.WriteFloat32(out, v.SecondField)
        ///     out = bebop.WriteByte(out, 3)
        ///     out = v.ThirdField.Encode(out)
        ///     bebop.WriteMessageLength(out, lengthPlaceholder)
        ///     return out
        ///   }
        ///   
        /// </summary>
        private void WriteMessageEncode(IndentedStringBuilder builder, IDefinition definition)
        {
            builder.AppendLine($"func (v *{definition.Name.ToPascalCase()}) Encode(out []byte) []byte {{");
            builder.Indent(IndentSpeces);

            builder.AppendLine("lengthPlaceholder := bebop.WriteMessageLengthPlaceholder(out)");
            builder.AppendLine("out = lengthPlaceholder");

            foreach (IField field in definition.Fields)
            {
                builder.AppendLine($"out = bebop.WriteByte(out, {field.ConstantValue})");
                builder.AppendLine(FieldEncodeString(field.Type, $"v.{field.Name.ToPascalCase()}"));
            }

            builder.AppendLine("bebop.WriteMessageLength(out, lengthPlaceholder)");
            builder.AppendLine("return out");

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");
            builder.AppendLine();
        }

        private void WriteMessageDecode(IndentedStringBuilder builder, IDefinition definition)
        {
        }

        private void WriteNamelessTypesFunctions(IndentedStringBuilder builder)
        {
            foreach (var kvp in _NamelessTypes)
            {
                switch (kvp.Value.Type)
                {
                case ArrayType at:
                    WriteArrayEncode(builder, kvp.Key, at, kvp.Value.MangledName);
                    WriteArrayDecode(builder, kvp.Key, at, kvp.Value.MangledName);
                    break;

                case MapType mt:
                    WriteMapEncode(builder, kvp.Key, mt, kvp.Value.MangledName);
                    WriteMapDecode(builder, kvp.Key, mt, kvp.Value.MangledName);
                    break;
                }
            }
        }

        /// <summary>
        /// Example output:
        ///
        ///   func encode__Ss_string(out []byte, value []string) []byte {
        ///     out = bebop.WriteArrayLength(len(value))
        ///     for _, item := range value {
        ///       out = bebop.WriteString(out, item)
        ///     }
        ///     return out
        ///   }
        ///   
        /// </summary>
        private void WriteArrayEncode(IndentedStringBuilder builder, string typename, ArrayType at, string mangledName)
        {
            builder.AppendLine($"func encode{mangledName}(out []byte, value {typename}) []byte {{");
            builder.Indent(IndentSpeces);

            builder.AppendLine("out = bebop.WriteArrayLength(out, len(value))");
            builder.AppendLine("for _, item := range value {");
            builder.Indent(IndentSpeces);

            builder.AppendLine(FieldEncodeString(at.MemberType, "item"));

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");
            builder.AppendLine("return out");

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");
        }

        private void WriteArrayDecode(IndentedStringBuilder builder, string typename, ArrayType at, string mangledName)
        {
        }

        /// <summary>
        /// Example output:
        ///
        ///   func encode__S_string_s_int32(out []byte, value map[string]int32) []byte {
        ///     out = bebop.WriteArrayLength(len(value))
        ///     for key, value := range value {
        ///       out = bebop.WriteString(out, key)
        ///       out = bebop.WriteInt32(out, value)
        ///     }
        ///     return out
        ///   }
        ///   
        /// </summary>
        private void WriteMapEncode(IndentedStringBuilder builder, string typename, MapType mt, string mangledName)
        {
            builder.AppendLine($"func encode{mangledName}(out []byte, value {typename}) []byte {{");
            builder.Indent(IndentSpeces);

            builder.AppendLine("out = bebop.WriteArrayLength(out, len(value))");
            builder.AppendLine("for key, value := range value {");
            builder.Indent(IndentSpeces);

            builder.AppendLine(FieldEncodeString(mt.KeyType, "key"));
            builder.AppendLine(FieldEncodeString(mt.ValueType, "value"));

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");
            builder.AppendLine("return out");

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");
        }

        private void WriteMapDecode(IndentedStringBuilder builder, string typename, MapType mt, string mangledName)
        {
        }

        /// <summary>
        /// Example output:
        ///
        ///   // A comment describing SomeType.
        ///   //
        //    // Deprecated: SomeType is no longer supported.
        /// </summary>
        private void WriteDocumentation(IndentedStringBuilder builder, string? documentation, string? deprecatedMessage)
        {
            bool hasDoc = !string.IsNullOrWhiteSpace(documentation);
            bool isDeprecated = !string.IsNullOrWhiteSpace(deprecatedMessage);

            if (hasDoc)
            {
                string[] lines = documentation!.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    builder.AppendLine($"// {line}");
                }
            }

            if (hasDoc && isDeprecated)
            {
                builder.AppendLine($"//");
            }

            if (isDeprecated)
            {
                builder.AppendLine($"// Deprecated: {deprecatedMessage}");
            }
        }

        private string TypeName(TypeBase type)
        {
            return type switch
            {
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => "bool",
                    BaseType.Byte => "byte",
                    BaseType.UInt16 => "uint16",
                    BaseType.Int16 => "int16",
                    BaseType.UInt32 => "uint32",
                    BaseType.Int32 => "int32",
                    BaseType.UInt64 => "uint64",
                    BaseType.Int64 => "int64",
                    BaseType.Float32 => "float32",
                    BaseType.Float64 => "float64",
                    BaseType.String => "string",
                    BaseType.Guid => "uuid.UUID",
                    BaseType.Date => "bebop.Timestamp",
                    _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                },
                ArrayType at => $"[]{TypeName(at.MemberType)}",
                MapType mt => $"map[{TypeName(mt.KeyType)}]{TypeName(mt.ValueType)}",
                DefinedType dt when IsEnum(dt) => dt.Name.ToPascalCase(),
                DefinedType dt => $"*{dt.Name.ToPascalCase()}",
                _ => throw new InvalidOperationException($"GetTypeName: {type}")
            };
        }

        private bool IsEnum(DefinedType dt)
        {
            return Schema.Definitions[dt.Name].Kind == AggregateKind.Enum;
        }

        private string FieldEncodeString(TypeBase type, string fieldName)
        {
            return type switch
            {
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => $"out = bebop.WriteBool(out, {fieldName})",
                    BaseType.Byte => $"out = bebop.WriteByte(out, {fieldName})",
                    BaseType.UInt16 => $"out = bebop.WriteUInt16(out, {fieldName})",
                    BaseType.Int16 => $"out = bebop.WriteInt16(out, {fieldName})",
                    BaseType.UInt32 => $"out = bebop.WriteUInt32(out, {fieldName})",
                    BaseType.Int32 => $"out = bebop.WriteInt32(out, {fieldName})",
                    BaseType.UInt64 => $"out = bebop.WriteUInt64(out, {fieldName})",
                    BaseType.Int64 => $"out = bebop.WriteInt64(out, {fieldName})",
                    BaseType.Float32 => $"out = bebop.WriteFloat32(out, {fieldName})",
                    BaseType.Float64 => $"out = bebop.WriteFloat64(out, {fieldName})",
                    BaseType.String => $"out = bebop.WriteString(out, {fieldName})",
                    BaseType.Guid => $"out = bebop.WriteGUID(out, {fieldName})",
                    BaseType.Date => $"out = bebop.WriteTimestamp(out, {fieldName})",
                    _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                },
                ArrayType at => $"out = encode{GetTypeMangledName(type)}(out, {fieldName})",
                MapType mt => $"out = encode{GetTypeMangledName(type)}(out, {fieldName})",
                DefinedType dt when IsEnum(dt) => $"out = bebop.WriteUInt32(out, uint32({fieldName}))",
                DefinedType dt => $"out = {fieldName}.Encode(out)",
                _ => throw new InvalidOperationException($"GetTypeName: {type}")
            };
        }
    }
}
