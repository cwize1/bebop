﻿using System.Collections.Generic;

namespace Core.Meta.Interfaces
{
    /// <summary>
    /// Represents the contents of a textual Bebop schema 
    /// </summary>
    public interface ISchema
    {
        public SchemaOptions Options { get; }
        /// <summary>
        /// An optional namespace that is provided to the compiler.
        /// </summary>
        public string Namespace => Options.Namepace;
        /// <summary>
        /// A collection of data structures defined in the schema
        /// </summary>
        public Dictionary<string, IDefinition> Definitions { get; }
        /// <summary>
        /// Validates that the schema is made up of well-formed values.
        /// </summary>
        public void Validate();

    }
}
