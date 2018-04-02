using System;

namespace Legit
{
    public sealed class PackFileFormatException : Exception
    {
        public PackFileFormatException(string message)
            : base(message)
        {
        }
    }
}