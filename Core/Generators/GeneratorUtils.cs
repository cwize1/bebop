﻿using System;
using System.Collections.Generic;
using Core.Generators.CPlusPlus;
using Core.Generators.CSharp;
using Core.Generators.Dart;
using Core.Generators.Go;
using Core.Generators.TypeScript;
using Core.Meta;
using Core.Meta.Extensions;
using Core.Meta.Interfaces;

namespace Core.Generators
{
    public static class GeneratorUtils
    {
        /// <summary>
        /// Gets the formatted base class name for a <see cref="UnionBranch"/>.
        /// </summary>
        /// <returns>The base class name.</returns>
        /// <remarks>
        ///  Used by the <see cref="CSharpGenerator"/> and other languages where "Base" indicates a class that can be inherited.
        /// </remarks>
        public static string BaseClassName(this UnionBranch branch) => $"Base{branch.Definition.Name.ToPascalCase()}";
        
        /// <summary>
        /// Gets the generic argument index for a branch of a union. 
        /// </summary>
        /// <returns>The index of the union as a generic positional argument.</returns>
        /// <remarks>
        ///  Generic arguments start at "0" whereas Bebop union branches start at "1". This just offsets the discriminator by 1 to retrieve the correct index. 
        /// </remarks>
        public static int GenericIndex(this UnionBranch branch) => branch.Discriminator - 1;

        /// <summary>
        /// A dictionary that contains generators.
        /// </summary>
        /// <remarks>
        /// Generators are keyed via their commandline alias.
        /// </remarks>
        public static Dictionary<string, Func<ISchema, Generator>> ImplementedGenerators  = new()
        {
            { "ts", s => new TypeScriptGenerator(s) },
            { "cs", s => new CSharpGenerator(s) },
            { "dart", s => new DartGenerator(s) },
            { "go", s => new GoGenerator(s) },
            { "cpp", s => new CPlusPlusGenerator(s) },
        };

        public static Dictionary<string, string> ImplementedGeneratorNames  = new()
        {
            { "ts", "TypeScript" },
            { "cs", "C#" },
            { "dart", "Dart" },
            { "go", "Go" },
            { "cpp", "C++" },
        };

        /// <summary>
        /// Returns a loop variable name based on the provided loop <paramref name="depth"/>
        /// </summary>
        /// <param name="depth">The depth of the loop</param>
        /// <returns>for 0-3 an actual letter is returned, for anything greater the depth prefixed with "i" is returned.</returns>
        public static string LoopVariable(int depth)
        {
            return $"i{depth}";
        }
    }
}
