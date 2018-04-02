using System;
using System.IO;
using Xunit;

namespace Legit
{
    public sealed class ObjectIdTests
    {
        [Theory]
        [InlineData("0000000000000000000000000000000000000000")]
        [InlineData("0102030405060708091011121314151617181920")]
        [InlineData("abcdefabcdef123456789abcdefabcdef0123456")]
        public void TryParse_handles_valid_hashes(string sha1)
        {
            Assert.True(ObjectId.TryParse(sha1, out var id));
            Assert.Equal(sha1.ToLower(), id.ToString());
        }

        [Theory]
        [InlineData("00000000000000000000000000000000000000")]
        [InlineData("000000000000000000000000000000000000000")]
        [InlineData("ABCDEFABCDEF123456789ABCDEFABCDEF0123456")]
        [InlineData("01020304050607080910111213141516171819200")]
        [InlineData("010203040506070809101112131415161718192001")]
        [InlineData("ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ")]
        [InlineData("  0000000000000000000000000000000000000000  ")]
        public void TryParse_identifies_invalid_hashes(string sha1)
        {
            Assert.False(ObjectId.TryParse(sha1, out _));
        }

        [Theory]
        [InlineData("0000000000000000000000000000000000000000", 0)]
        [InlineData("0000000000000000000000000000000000000000__", 0)]
        [InlineData("_0102030405060708091011121314151617181920", 1)]
        [InlineData("_0102030405060708091011121314151617181920_", 1)]
        [InlineData("__0102030405060708091011121314151617181920", 2)]
        [InlineData("__0102030405060708091011121314151617181920__", 2)]
        public void TryParse_with_offset_handles_valid_hashes(string sha1, int offset)
        {
            Assert.True(ObjectId.TryParse(sha1, offset, out var id));
            Assert.Equal(
                sha1.Substring(offset, 40),
                id.ToString());
        }

        [Theory]
        [InlineData("0000000000000000000000000000000000000000")]
        [InlineData("0102030405060708091011121314151617181920")]
        [InlineData("abcdefabcdef123456789abcdefabcdef0123456")]
        public void Parse_handles_valid_hashes(string sha1)
        {
            Assert.Equal(
                sha1.ToLower(),
                ObjectId.Parse(sha1).ToString());
        }

        [Theory]
        [InlineData("00000000000000000000000000000000000000")]
        [InlineData("000000000000000000000000000000000000000")]
        [InlineData("01020304050607080910111213141516171819200")]
        [InlineData("010203040506070809101112131415161718192001")]
        [InlineData("ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ")]
        [InlineData("  0000000000000000000000000000000000000000  ")]
        public void Parse_throws_for_invalid_hashes(string sha1)
        {
            Assert.Throws<FormatException>(() => ObjectId.Parse(sha1));
        }

        [Theory]
        [InlineData("0000000000000000000000000000000000000000")]
        [InlineData("0102030405060708091011121314151617181920")]
        [InlineData("abcdefabcdef123456789abcdefabcdef0123456")]
        public void IsValid_identifies_valid_hashes(string sha1)
        {
            Assert.True(ObjectId.IsValid(sha1));
        }

        [Theory]
        [InlineData("00000000000000000000000000000000000000")]
        [InlineData("000000000000000000000000000000000000000")]
        [InlineData("abcdefABCEF0123456789abcdefabcdef0123456")]
        [InlineData("01020304050607080910111213141516171819200")]
        [InlineData("010203040506070809101112131415161718192001")]
        [InlineData("ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ")]
        [InlineData("  0000000000000000000000000000000000000000  ")]
        public void IsValid_identifies_invalid_hashes(string sha1)
        {
            Assert.False(ObjectId.IsValid(sha1));
        }

        [Theory]
        [InlineData("0000000000000000000000000000000000000000", 0)]
        [InlineData("0000000000000000000000000000000000000000__", 0)]
        [InlineData("_0102030405060708091011121314151617181920", 1)]
        [InlineData("_0102030405060708091011121314151617181920_", 1)]
        [InlineData("__0102030405060708091011121314151617181920", 2)]
        [InlineData("__0102030405060708091011121314151617181920__", 2)]
        public void Parse_with_offset_handles_valid_hashes(string sha1, int offset)
        {
            Assert.Equal(
                sha1.Substring(offset, 40),
                ObjectId.Parse(sha1, offset).ToString());
        }

        [Fact]
        public void Equivalent_ids_are_equal()
        {
            Assert.Equal(
                ObjectId.Parse("0102030405060708091011121314151617181920"),
                ObjectId.Parse("0102030405060708091011121314151617181920"));

            Assert.Equal(
                ObjectId.Parse("abcdefabcdefabcdefabcdefabcdefabcdefabcd"),
                ObjectId.Parse("ABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCD".ToLower()));
        }

        [Fact]
        public void Different_ids_are_not_equal()
        {
            Assert.NotEqual(
                ObjectId.Parse("0000000000000000000000000000000000000000"),
                ObjectId.Parse("0102030405060708091011121314151617181920"));
        }

        [Fact]
        public void Equivalent_ids_have_equal_hash_codes()
        {
            Assert.Equal(
                ObjectId.Parse("0102030405060708091011121314151617181920").GetHashCode(),
                ObjectId.Parse("0102030405060708091011121314151617181920").GetHashCode());

            Assert.Equal(
                ObjectId.Parse("abcdefabcdefabcdefabcdefabcdefabcdefabcd").GetHashCode(),
                ObjectId.Parse("ABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCD".ToLower()).GetHashCode());
        }

        [Fact]
        public void Different_ids_have_different_hash_codes()
        {
            Assert.NotEqual(
                ObjectId.Parse("0000000000000000000000000000000000000000").GetHashCode(),
                ObjectId.Parse("abcdefabcdefabcdefabcdefabcdefabcdefabcd").GetHashCode());
        }

        [Fact]
        public void Parse_from_stream()
        {
            var stream = new MemoryStream(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20});
            var objectId = ObjectId.Parse(stream);
            Assert.Equal("0102030405060708090a0b0c0d0e0f1011121314", objectId.ToString());
        }

        [Fact]
        public void ToString_works()
        {
            var stream = new MemoryStream(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20});
            var objectId = ObjectId.Parse(stream);
            Assert.Equal("0102030405060708090a0b0c0d0e0f1011121314", objectId.ToString());
        }
    }
}
