using Core.Meta;
using Core.Meta.Extensions;
using Core.Meta.Interfaces;
using System;

namespace Core.Generators.Go
{
    public class GoGenerator : Generator
    {
        private const int IndentSpeces = 1;

        public GoGenerator(ISchema schema) :
            base(schema)
        {
        }

        public override string Compile()
        {
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
                WriteDefinition(builder, definition);
            }

            return builder.ToString();
        }

        public override void WriteAuxiliaryFiles(string outputPath)
        {
            // Nothing to do.
        }

        private void WriteDefinition(IndentedStringBuilder builder, IDefinition definition)
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

        private void WriteAggregateTypeDefinition(IndentedStringBuilder builder, IDefinition definition)
        {
            string structName = definition.Name.ToPascalCase();

            WriteDocumentation(builder, definition.Documentation, null);
            builder.AppendLine($"type {structName} struct {{");
            builder.Indent(IndentSpeces);

            foreach (IField field in definition.Fields)
            {
                WriteAggregateTypeField(builder, field);
            }

            builder.Dedent(IndentSpeces);
            builder.AppendLine("}");
            builder.AppendLine();
        }

        private void WriteAggregateTypeField(IndentedStringBuilder builder, IField field)
        {
            WriteDocumentation(builder, field.Documentation, field.DeprecatedAttribute?.Value);

            string typeName = TypeName(field.Type);
            string fieldName = field.Name.ToPascalCase();

            builder.AppendLine($"{fieldName} {typeName}");
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
                    BaseType.UInt32 => "uint32",
                    BaseType.Int32 => "int32",
                    BaseType.Float32 => "float32",
                    BaseType.Float64 => "float64",
                    BaseType.String => "string",
                    BaseType.Guid => "uuid.UUID",
                    BaseType.UInt16 => "uint16",
                    BaseType.Int16 => "int16",
                    BaseType.UInt64 => "uint64",
                    BaseType.Int64 => "int64",
                    BaseType.Date => "bebop.Timestamp",
                    _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                },
                ArrayType at => $"[]{TypeName(at.MemberType)}",
                MapType mt => $"map[{TypeName(mt.KeyType)}]{TypeName(mt.ValueType)}",
                DefinedType dt => dt.Name.ToPascalCase(),
                _ => throw new InvalidOperationException($"GetTypeName: {type}")
            };
        }
    }
}
