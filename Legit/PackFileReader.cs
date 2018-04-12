using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Ionic.Zlib;

namespace Legit
{
    /// <summary>
    /// Reads a pack file and its index, enabling iterating and searching content.
    /// </summary>
    public sealed class PackFileReader
    {
        private const uint PackSignature = 0x5041434b; // 'P' 'A' 'C' 'K'
        private const uint PackVersion = 2;

        private const uint IdxSignature = 0xFF744F63;
        private const uint IdxVersion = 2;

        private const int Level1Offset = 8;
        private const int Level2BaseOffset = Level1Offset + 256*sizeof(uint);
        private const int ObjectLength = 20;

        private readonly Stream _idxStream;
        private readonly Stream _packStream;
        private readonly BigEndianBinaryReader _idxReader;
        private readonly BigEndianBinaryReader _packReader;

        public uint ObjectCount { get; }

        public PackFileReader(Stream idxStream, Stream packStream)
        {
            _idxStream = idxStream;
            _packStream = packStream;

            _idxReader = new BigEndianBinaryReader(idxStream);
            _packReader = new BigEndianBinaryReader(packStream);

            if (_packReader.ReadUInt32BE() != PackSignature)
                throw new FormatException("Pack does not have expected signature.");

            if (_packReader.ReadUInt32BE() != PackVersion)
                throw new FormatException("Pack does not have expected version.");

            ObjectCount = _packReader.ReadUInt32BE();

            if (_idxReader.ReadUInt32BE() != IdxSignature)
                throw new FormatException("Idx does not have expected signature.");

            if (_idxReader.ReadUInt32BE() != IdxVersion)
                throw new FormatException("Idx does not have expected version.");
        }

        private uint Level3BaseOffset => Level2BaseOffset + (ObjectCount * ObjectLength);

        private uint Level4BaseOffset => Level3BaseOffset + (ObjectCount * sizeof(uint));

        public IEnumerable<ObjectId> ObjectIds
        {
            get
            {
                _idxStream.Position = Level2BaseOffset;

                for (var i = 0; i < ObjectCount; i++)
                    yield return ObjectId.Parse(_idxStream);
            }
        }

        public IEnumerable<(ObjectId, PackFileEntry)> Entries
        {
            get
            {
                var objectIdIndex = Level2BaseOffset;
                var entryOffsetIndex = Level4BaseOffset;

                for (var i = 0; i < ObjectCount; i++)
                {
                    _idxStream.Position = objectIdIndex;
                    var objectId = ObjectId.Parse(_idxStream);

                    _idxStream.Position = entryOffsetIndex;
                    var entryOffset = _idxReader.ReadUInt32BE();
                    var entry = ReadPackEntry(entryOffset);

                    yield return (objectId, entry);

                    objectIdIndex += 20;
                    entryOffsetIndex += 4;
                }
            }
        }

        public bool TryReadEntry(ObjectId objectId, out PackFileEntry entry)
        {
            // level one is determined by the first byte of the object id
            var b0 = objectId.Byte0;

            // level two gives the index of the object within the file
            var objectIndex = FindLevel2(objectId, b0);

            if (objectIndex == -1)
            {
                entry = default;
                return false;
            }

            Debug.Assert(objectIndex >= 0 && objectIndex < ObjectCount);

            // level three contains CRC values per object
//            var crcIndex = Level3BaseOffset + (objectIndex * sizeof(uint));

            // level four contains packfile offset per object
            var offsetIndex = Level4BaseOffset + (objectIndex * sizeof(uint));

            _idxStream.Position = offsetIndex;

            var index = _idxReader.ReadUInt32BE();

            // TODO level five is present for pack files > 2 GB

            entry = ReadPackEntry(index);
            return true;
        }

        private long FindLevel2(ObjectId objectId, byte b0)
        {
            uint lower, upper;

            if (b0 == 0)
            {
                _idxStream.Position = Level1Offset;
                lower = 0;
                upper = _idxReader.ReadUInt32BE();
            }
            else
            {
                _idxStream.Position = Level1Offset - 4 + (b0 << 2);
                lower = _idxReader.ReadUInt32BE();
                upper = _idxReader.ReadUInt32BE();
            }

            var items = upper - lower;

            if (items == 0)
                return -1;

            // TODO for packfiles with many objects, it may be better to compute the first 'mid' value based on the second byte

            do
            {
                var mid = (lower + upper) >> 1;

                var offset = Level2BaseOffset + mid*ObjectLength;

                _idxStream.Position = offset;

                var c = objectId.CompareTo(_idxReader);

                if (c < 0)
                    upper = mid;
                else if (c == 0)
                    return mid;
                else
                    lower = mid + 1;
            }
            while (lower < upper);

            return -1;
        }

        public Content ReadContent(PackFileEntry entry)
        {
            _packStream.Position = entry.DataOffset;

            switch (entry.Type)
            {
                case PackFileEntryType.AnnotatedTag:
                case PackFileEntryType.Blob:
                case PackFileEntryType.Commit:
                case PackFileEntryType.Tree:
                {
                    using (var inflate = new ZlibStream(_packStream, CompressionMode.Decompress, leaveOpen: true))
                    {
                        var buffer = inflate.ReadBytes(checked((int)entry.Size));
                        return new Content((ContentType)entry.Type, buffer);
                    }
                }
                case PackFileEntryType.OffsetDelta:
                {
                    var baseRelativeOffset = ReadOffset();
                    var deltaDataOffset = _packStream.Position;
                    var baseOffset = entry.EntryOffset - baseRelativeOffset;
                    var baseEntry = ReadPackEntry(baseOffset);

                    // TODO does the stream need to be deflated here?

                    return ComputeDeltaObject(baseEntry, deltaDataOffset);

                    long ReadOffset()
                    {
                        var b = _packStream.ReadByte();

                        if (b == -1)
                            throw new EndOfStreamException();

                        long num = b & 0b0111_1111;

                        while (b >= 0b1000_0000)
                        {
                            num += 1;

                            b = _packStream.ReadByte();

                            if (b == -1)
                                throw new EndOfStreamException();

                            num <<= 7;
                            num |= b & 0b0111_1111u;
                        }

                        return num;
                    }
                }
                case PackFileEntryType.ReferenceDelta:
                {
                    // Read 20 bytes for the base object id
                    var baseObjectId = ObjectId.Parse(_packStream);
                    var deltaDataOffset = _packStream.Position;

                    // Look up the base position in the stream
                    if (!TryReadEntry(baseObjectId, out var baseEntry))
                        throw new PackFileFormatException("Reference delta has unknown object ID");

                    return ComputeDeltaObject(baseEntry, deltaDataOffset);
                }
                default:
                {
                    throw new NotImplementedException($"Object type {entry.Type} is not expected.");
                }
            }
        }

        private Content ComputeDeltaObject(PackFileEntry baseEntry, long deltaDataOffset)
        {
            var baseObject = ReadContent(baseEntry);

            return new Content(baseObject.Type, ApplyDelta());

            byte[] ApplyDelta()
            {
                var baseBytes = baseObject.Bytes;

                _packStream.Position = deltaDataOffset;

                using (var stream = new ZlibStream(_packStream, CompressionMode.Decompress, leaveOpen: true))
                using (var reader = new BinaryReader(stream))
                {
                    var expectedBaseLength = ReadLength();

                    if (expectedBaseLength != unchecked((ulong)baseObject.Bytes.Length))
                        throw new PackFileFormatException($"Base object did not have expected length. Expected {expectedBaseLength}, received {baseObject.Bytes.Length}.");

                    // Length of the composed output
                    var outputLength = ReadLength();

                    ulong ReadLength()
                    {
                        var length = 0UL;
                        var shift = 0;

                        while (true)
                        {
                            var b = reader.ReadByte();

                            length |= (ulong)(b & 0x7f) << shift;

                            if ((b & 0x80) == 0)
                                return length;

                            shift += 7;
                        }
                    }

                    var outputBytes = new byte[outputLength];
                    var outputIndex = 0u;

                    while (outputIndex < outputLength)
                    {
                        var b = reader.ReadByte();

                        if (b >= 0b1000_0000)
                        {
                            // copy -- from base to output

                            uint copyFromOffset = 0;

                            if ((b & 0b0001) != 0) copyFromOffset = reader.ReadByte();
                            if ((b & 0b0010) != 0) copyFromOffset |= (uint)reader.ReadByte() << 8;
                            if ((b & 0b0100) != 0) copyFromOffset |= (uint)reader.ReadByte() << 16;
                            if ((b & 0b1000) != 0) copyFromOffset |= (uint)reader.ReadByte() << 24;

                            uint copyLength = 0;

                            if ((b & 0b0001_0000) != 0) copyLength = reader.ReadByte();
                            if ((b & 0b0010_0000) != 0) copyLength |= (uint)reader.ReadByte() << 8;
                            if ((b & 0b0100_0000) != 0) copyLength |= (uint)reader.ReadByte() << 16;

                            // TODO experiment with methods of copying bytes
                            Array.Copy(baseBytes, copyFromOffset, outputBytes, outputIndex, copyLength);

                            outputIndex += copyLength;
                        }
                        else if (b != 0)
                        {
                            // insert
                            var insertLength = b;

                            var read = 0;
                            while (read != insertLength)
                                read += stream.Read(outputBytes, checked((int)outputIndex) + read, insertLength - read);

                            outputIndex += insertLength;
                        }
                        else
                        {
                            throw new PackFileFormatException("Invalid zero delta command.");
                        }
                    }

                    return outputBytes;
                }
            }
        }

        private PackFileEntry ReadPackEntry(long offset)
        {
            _packStream.Position = offset;

            var b = _packReader.ReadByte();

            var type = (PackFileEntryType)((b & 0b0111_0000) >> 4);

            // TODO is (b & 0b1000_0000) != 0 faster than b >= 0b1000_0000

            long size = b & 0b0000_1111;
            var shift = 4;
            while (b >= 0b1000_0000)
            {
                b = _packReader.ReadByte();
                size |= (long)(b & 0b0111_1111) << shift;
                shift += 7;
            }

            return new PackFileEntry(size, type, offset, _packStream.Position);
        }
    }
}
