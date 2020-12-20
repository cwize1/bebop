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
        public GoGenerator(ISchema schema) : base(schema)
        {
        }

        public override string Compile()
        {
            throw new NotImplementedException();
        }

        public override void WriteAuxiliaryFiles(string outputPath)
        {
            throw new NotImplementedException();
        }
    }
}
