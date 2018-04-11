using System;
using System.Collections.Generic;

namespace Legit
{
    public static class ContentExtensions
    {
        public static IEnumerable<TreeEntry> ParseTree(this Content content)
        {
            if (content.Type != ContentType.Tree)
                throw new ArgumentException("Content must be a Tree.", nameof(content));

            var bytes = content.Bytes;
            var offset = 0;

            while (offset < bytes.Length)
            {
                var mode = 0;
                while (true)
                {
                    var b = bytes[offset++];
                    if (b == ' ')
                        break;
                    mode <<= 3;
                    mode += b - '0';
                }

                // TODO maybe instead of copying, use ArraySegment<byte>
                var pathBytes = ArrayUtil.GetNullTerminatedBytes(bytes, offset);
                if (pathBytes == null)
                    throw new PackFileFormatException("Tree object path did not terminate.");
                offset += pathBytes.Length + 1;

                var objectId = ObjectId.Parse(bytes, offset);

                offset += 20;

                yield return new TreeEntry(mode, pathBytes, objectId);
            }
        }
    }
}
