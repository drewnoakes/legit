using System.IO;
using JetBrains.Annotations;

namespace Legit
{
    internal static class StreamExtensions
    {
        [NotNull]
        public static byte[] ReadBytes([NotNull] this Stream stream, int count)
        {
            var buffer = new byte[count];

            stream.ReadBytes(buffer, offset: 0, count);

            return buffer;
        }

        public static void ReadBytes([NotNull] this Stream stream, [NotNull] byte[] buffer, int offset, int count)
        {
            while (offset != count)
            {
                var read = stream.Read(buffer, offset, count - offset);

                if (read == 0)
                    throw new EndOfStreamException();

                offset += read;
            }
        }
    }
}
