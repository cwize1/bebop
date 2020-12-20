using Core.Meta.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Generators.Go
{
    public class GoGenerator : Generator
    {
        public GoGenerator(ISchema schema) :
            base(schema)
        {
        }

        public override string Compile()
        {
            var builder = new IndentedStringBuilder();

            if (string.IsNullOrWhiteSpace(Schema.Options.GoPkg))
            {
                throw new Exception("Go requies a package name to be specified.");
            }

            builder.AppendLine($"package ${Schema.Options.GoPkg}");

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

        }
    }
}
