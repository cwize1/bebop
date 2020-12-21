using System;
using System.Linq;
using System.Text;

namespace Core.Generators
{
    public class IndentedStringBuilder
    {
        private int Spaces { get; set; }
        private StringBuilder Builder { get; }
        static readonly string[] _newlines = new[] { "\r\n", "\r", "\n" };
        private char IndentChar { get; set; }

        public IndentedStringBuilder(int spaces = 0, char indentChar = ' ')
        {
            Spaces = spaces;
            Builder = new StringBuilder();
            IndentChar = indentChar;
        }

        public IndentedStringBuilder AppendLine()
        {
            return AppendLine(Environment.NewLine);
        }

        public IndentedStringBuilder AppendLine(string text)
        {
            var indent = new string(IndentChar, Spaces);
            var lines = text.Split(_newlines, StringSplitOptions.None);
            var indentedLines = lines.Select(x => (indent + x).TrimEnd()).ToArray();
            var indentedText = string.Join(Environment.NewLine, indentedLines).TrimEnd();
            Builder.AppendLine(indentedText);
            return this;
        }

        public IndentedStringBuilder Indent(int addSpaces = 0)
        {
            Spaces = Math.Max(0, Spaces + addSpaces);
            return this;
        }

        public IndentedStringBuilder Dedent(int removeSpaces = 0)
        {
            Spaces = Math.Max(0, Spaces - removeSpaces);
            return this;
        }

        public override string ToString()
        {
            return Builder.ToString();
        }
    }
}
