﻿using Core.Meta;
using Core.Meta.Extensions;
using Core.Meta.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Generators.Go
{
    public class GoGenerator : Generator
    {
        // Go strongly prefers the use of tabs over spaces.
        private const char IndentChar = '\t'; 

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

            var builder = new IndentedStringBuilder(indentChar: IndentChar);

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

        /// <summary>
        /// Example output:
        ///
        ///   func (v *SomeClass) Decode(in []byte) ([]byte, error) {
        ///     var err error
        ///     v.FirstField, in, err = bebop.ReadBool(in)
        ///     if err != nil {
        ///         return in, err
        ///     }
        ///     v.SecondField, in, err = bebop.WriteFloat32(in)
        ///     if err != nil {
        ///         return in, err
        ///     }
        ///     in, err = v.ThirdField.Encode(out)
        ///     if err != nil {
        ///         return in, err
        ///     }
        ///     return in, nil
        ///   }
        ///   
        /// </summary>
        private void WriteStructDecode(IndentedStringBuilder builder, IDefinition definition)
        {
            builder.AppendLine($"func (v *{definition.Name.ToPascalCase()}) Decode(in []byte) ([]byte, error) {{");
            builder.Indent(IndentSpeces);

            builder.AppendLine("var err error");

            foreach (IField field in definition.Fields)
            {
                builder.AppendLine(FieldDecodeString(field.Type, $"v.{field.Name.ToPascalCase()}"));
                builder.AppendLine("if err != nil {");
                builder.AppendLine(IndentChar + "return in, err");
                builder.AppendLine("}");
            }

            builder.AppendLine("return in, nil");

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");
            builder.AppendLine();
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
        ///     out = bebop.WriteByte(out, 0)
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

            builder.AppendLine("out = bebop.WriteByte(out, 0)");
            builder.AppendLine("bebop.WriteMessageLength(out, lengthPlaceholder)");
            builder.AppendLine("return out");

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");
            builder.AppendLine();
        }

        /// <summary>
        /// Example output:
        ///
        ///     func (v *SomeClass) Decode(in []byte) ([]byte, error) {
        ///         var err error
        ///         var messageLength int
        ///         messageLength, in, err = bebop.ReadMessageLength(in)
        ///         if err != nil {
        ///             return in, err
        ///         }
        ///         bodyStart := in
        ///         Loop:
        ///         for {
        ///             var tag byte
        ///             tag, in, err = bebop.ReadByte(in)
        ///             if err != nil {
        ///                 return in, err
        ///             }
        ///             switch tag {
        ///             case 1:
        ///                 v.FirstField, in, err = bebop.ReadBool(in)
        ///                 if err != nil {
        ///                     return in, err
        ///                 }
        ///             case 2:
        ///                 v.SecondField, in, err = bebop.WriteFloat32(in)
        ///                 if err != nil {
        ///                     return in, err
        ///                 }
        ///             case 3:
        ///                 in, err = v.ThirdField.Encode(out)
        ///                 if err != nil {
        ///                     return in, err
        ///                 }
        ///             default:
        ///                 break Loop
        ///             }
        ///         }
        ///         if len(bodyStart) - len(in) > messageLength {
        ///             return in, bebop.ErrMessageBodyOverrun
        ///         }
        ///         return in, nil
        ///     }
        ///     
        /// </summary>
        private void WriteMessageDecode(IndentedStringBuilder builder, IDefinition definition)
        {
            builder.AppendLine($"func (v *{definition.Name.ToPascalCase()}) Decode(in []byte) ([]byte, error) {{");
            builder.Indent(IndentSpeces);

            builder.AppendLine("var err error");
            builder.AppendLine("var messageLength int");
            builder.AppendLine("messageLength, in, err = bebop.ReadMessageLength(in)");
            builder.AppendLine("if err != nil {\n\treturn in, err\n}");
            builder.AppendLine("bodyStart := in");
            builder.AppendLine("Loop:");
            builder.AppendLine("for {");
            builder.Indent(IndentSpeces);

            builder.AppendLine("var tag byte");
            builder.AppendLine("tag, in, err = bebop.ReadByte(in)");
            builder.AppendLine("if err != nil {\n\treturn in, err\n}");
            builder.AppendLine("switch tag {");

            foreach (IField field in definition.Fields)
            {
                builder.AppendLine($"case {field.ConstantValue}:");
                builder.Indent(IndentSpeces);

                builder.AppendLine(FieldDecodeString(field.Type, $"v.{field.Name.ToPascalCase()}"));
                builder.AppendLine("if err != nil {\n\treturn in, err\n}");

                builder.Dedent(IndentSpeces);
            }

            builder.AppendLine("default:");
            builder.AppendLine("\tbreak Loop");
            builder.AppendLine("}");

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");

            builder.AppendLine("if len(bodyStart) - len(in) > messageLength {");
            builder.AppendLine("\treturn in, bebop.ErrMessageBodyOverrun");
            builder.AppendLine("}");

            builder.AppendLine("return in, nil");

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");
            builder.AppendLine();
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
            builder.AppendLine();
        }

        /// <summary>
        /// Example output:
        ///
        ///     func decode__Ss_string(out []byte) ([]string, []byte, error) {
        ///         arrayLength, in, err := bebop.ReadArrayLength(in)
        ///         if err != nil {
        ///             return nil, in, err
        ///         }
        ///         v := make([]string, arrayLength)
        ///         for i := 0; i < arrayLength; i++ {
        ///             v[i], in, err = bebop.ReadString(in)
        ///             if err != nil {
        ///                 return nil, in, err
        ///             }
        ///         }
        ///         return v, in, nil
        ///     }
        ///     
        /// </summary>
        private void WriteArrayDecode(IndentedStringBuilder builder, string typename, ArrayType at, string mangledName)
        {
            builder.AppendLine($"func decode{mangledName}(in []byte) ({typename}, []byte, error) {{");
            builder.Indent(IndentSpeces);

            builder.AppendLine("arrayLength, in, err := bebop.ReadArrayLength(in)");
            builder.AppendLine("if err != nil {");
            builder.AppendLine(IndentChar + "return nil, in, err");
            builder.AppendLine("}");

            builder.AppendLine($"v := make({typename}, arrayLength)");
            builder.AppendLine("for i := 0; i < arrayLength; i++ {");
            builder.Indent(IndentSpeces);

            builder.AppendLine(FieldDecodeString(at.MemberType, $"v[i]"));
            builder.AppendLine("if err != nil {");
            builder.AppendLine(IndentChar + "return nil, in, err");
            builder.AppendLine("}");

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");
            builder.AppendLine("return v, in, nil");

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");
            builder.AppendLine();
        }

        /// <summary>
        /// Example output:
        ///
        ///   func encode_map_S_string_s_int32(out []byte, value map[string]int32) []byte {
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
            builder.AppendLine();
        }

        /// <summary>
        /// Example output:
        ///
        ///     func decode_map_S_string_s_int32(in []byte) (value map[string]int32, []byte, error)  {
        ///         arrayLength, in, err := bebop.ReadArrayLength(in)
        ///         if err != nil {
        ///             return nil, in, err
        ///         }
        ///         v := make(map[string]int32)
        ///         for i := 0; i < arrayLength; i++ {
        ///             var key string
        ///             key, in, err = bebop.ReadString(in)
        ///             if err != nil {
        ///                 return nil, in, err
        ///             }
        ///             var value int32
        ///             value, in, err = bebop.ReadInt32(in)
        ///             if err != nil {
        ///                 return nil, in, err
        ///             }
        ///             v[key] = value
        ///         }
        ///         return v, in, nil
        ///     }
        ///     
        /// </summary>
        private void WriteMapDecode(IndentedStringBuilder builder, string typename, MapType mt, string mangledName)
        {
            builder.AppendLine($"func decode{mangledName}(in []byte) ({typename}, []byte, error) {{");
            builder.Indent(IndentSpeces);

            builder.AppendLine("arrayLength, in, err := bebop.ReadArrayLength(in)");
            builder.AppendLine("if err != nil {");
            builder.AppendLine(IndentChar + "return nil, in, err");
            builder.AppendLine("}");

            builder.AppendLine($"v := make({typename})");
            builder.AppendLine("for i := 0; i < arrayLength; i++ {");
            builder.Indent(IndentSpeces);

            builder.AppendLine($"var key {TypeName(mt.KeyType)}");
            builder.AppendLine(FieldDecodeString(mt.KeyType, $"key"));
            builder.AppendLine("if err != nil {");
            builder.AppendLine(IndentChar + "return nil, in, err");
            builder.AppendLine("}");

            builder.AppendLine($"var value {TypeName(mt.ValueType)}");
            builder.AppendLine(FieldDecodeString(mt.ValueType, $"value"));
            builder.AppendLine("if err != nil {");
            builder.AppendLine(IndentChar + "return nil, in, err");
            builder.AppendLine("}");

            builder.AppendLine("v[key] = value");

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");
            builder.AppendLine("return v, in, nil");

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");
            builder.AppendLine();
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

        private string FieldDecodeString(TypeBase type, string fieldName)
        {
            return type switch
            {
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => $"{fieldName}, in, err = bebop.ReadBool(in)",
                    BaseType.Byte => $"{fieldName}, in, err = bebop.ReadByte(in)",
                    BaseType.UInt16 => $"{fieldName}, in, err = bebop.ReadUInt16(in)",
                    BaseType.Int16 => $"{fieldName}, in, err = bebop.ReadInt16(in)",
                    BaseType.UInt32 => $"{fieldName}, in, err = bebop.ReadUInt32(in)",
                    BaseType.Int32 => $"{fieldName}, in, err = bebop.ReadInt32(in)",
                    BaseType.UInt64 => $"{fieldName}, in, err = bebop.ReadUInt64(in)",
                    BaseType.Int64 => $"{fieldName}, in, err = bebop.ReadInt64(in)",
                    BaseType.Float32 => $"{fieldName}, in, err = bebop.ReadFloat32(in)",
                    BaseType.Float64 => $"{fieldName}, in, err = bebop.ReadFloat64(in)",
                    BaseType.String => $"{fieldName}, in, err = bebop.ReadString(in)",
                    BaseType.Guid => $"{fieldName}, in, err = bebop.ReadGUID(in)",
                    BaseType.Date => $"{fieldName}, in, err = bebop.ReadTimestamp(in)",
                    _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                },
                ArrayType at => $"{fieldName}, in, err = decode{GetTypeMangledName(type)}(in)",
                MapType mt => $"{fieldName}, in, err = decode{GetTypeMangledName(type)}(in)",
                DefinedType dt when IsEnum(dt) => $"{{\n\tvar tmp uint32\n\ttmp, in, err = bebop.ReadUInt32(in)\n\t{fieldName} = {dt.Name}(tmp)\n}}",
                DefinedType dt => $"{fieldName} = &{dt.Name}{{}}\nin, err = {fieldName}.Decode(in)",
                _ => throw new InvalidOperationException($"GetTypeName: {type}")
            };
        }
    }
}
