using System;
using System.Text;
using JetBrains.Annotations;

namespace Legit
{
    /// <summary>
    /// A piece of content in a git repo.
    /// </summary>
    /// <remarks>
    /// Models the byte content and type of this content.
    /// </remarks>
    public readonly struct Content
    {
        public ContentType Type { get; }
        [NotNull] public byte[] Bytes { get; }

        public Content(ContentType type, [NotNull] byte[] bytes)
        {
            Type = type;
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        }

        public string GetString(Encoding encoding = null)
            => (encoding ?? Encoding.UTF8).GetString(Bytes);

        public override string ToString() => $"{Bytes.Length} byte {Type} object";
    }

    /// <summary>
    /// Enumeration of possible object types that can be stored in a git repo.
    /// </summary>
    public enum ContentType
    {
        // NOTE these numbers must match the corresponding entries in PackFileEntryType

        Commit = 1,
        Tree = 2,
        Blob = 3,
        AnnotatedTag = 4
    }

    public readonly struct TreeEntry
    {
        // TODO can mode be uint16?
        public int Mode { get; }
        public byte[] PathBytes { get; }
        public ObjectId ObjectId { get; }

        public TreeEntry(int mode, byte[] pathBytes, ObjectId objectId)
        {
            Mode = mode;
            PathBytes = pathBytes;
            ObjectId = objectId;
        }
    }
}
