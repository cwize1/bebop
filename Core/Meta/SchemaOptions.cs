using System;

namespace Core.Meta
{
    public readonly struct SchemaOptions
    {
        public string Namepace { get; init; }

        public string GoPkg { get; init; }
    }
}