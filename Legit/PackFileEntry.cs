namespace Legit
{
    public readonly struct PackFileEntry
    {
        public long Size { get; }
        public PackFileEntryType Type { get; }
        public long EntryOffset { get; }
        public long DataOffset { get; }

        internal PackFileEntry(long size, PackFileEntryType type, long entryOffset, long dataOffset)
        {
            Size = size;
            Type = type;
            EntryOffset = entryOffset;
            DataOffset = dataOffset;
        }

        public override string ToString() => $"{Size} byte {Type} entry at offset {EntryOffset}";
    }

    /// <summary>
    /// Type of entry in pack file.
    /// </summary>
    public enum PackFileEntryType
    {
        Commit = 1,
        Tree = 2,
        Blob = 3,
        AnnotatedTag = 4,
        OffsetDelta = 6,
        ReferenceDelta = 7
    }
}
