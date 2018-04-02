using System;
using System.Text;
using JetBrains.Annotations;

namespace Legit
{
    public readonly struct GitObject
    {
        public GitObjectType Type { get; }
        [NotNull] public byte[] Bytes { get; }

        public GitObject(GitObjectType type, [NotNull] byte[] bytes)
        {
            Type = type;
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        }

        public string GetString(Encoding encoding = null)
            => (encoding ?? Encoding.UTF8).GetString(Bytes);

        public override string ToString() => $"{Bytes.Length} byte {Type} object";
    }

    public enum GitObjectType
    {
        // NOTE these numbers must match the corresponding entries in PackFileEntryType

        Commit = 1,
        Tree = 2,
        Blob = 3,
        AnnotatedTag = 4
    }
}
