using System.Diagnostics;
using System.IO;

namespace Legit
{
    public sealed class BigEndianBinaryReader
    {
        private readonly byte[] _buffer = new byte[4];
        private readonly Stream _stream;

        public BigEndianBinaryReader(Stream stream)
        {
            _stream = stream;
        }

        public byte ReadByte()
        {
            var num = _stream.ReadByte();
            if (num == -1)
                throw new EndOfStreamException();
            return (byte) num;
        }

        public uint ReadUInt32BE()
        {
            FillBuffer(4);

            return (uint) (_buffer[0] << 24 | _buffer[1] << 16 | _buffer[2] << 8 | _buffer[3]);
        }

        private void FillBuffer(int numBytes)
        {
            Debug.Assert(numBytes >= 0 && numBytes <= _buffer.Length);

            if (numBytes == 1)
            {
                var num = _stream.ReadByte();
                if (num == -1)
                    throw new EndOfStreamException();
                _buffer[0] = (byte) num;
            }
            else
            {
                var offset = 0;
                do
                {
                    var num = _stream.Read(_buffer, offset, numBytes - offset);
                    if (num == 0)
                        throw new EndOfStreamException();
                    offset += num;
                }
                while (offset < numBytes);
            }
        }
    }
}
