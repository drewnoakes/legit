using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Legit
{
    public sealed class PackFileReaderTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly FileStream _idxStream;
        private readonly FileStream _packStream;
        private readonly PackFileReader _packFile;

        public PackFileReaderTests(ITestOutputHelper output)
        {
            _output = output;

            // 0251b616d8c2e7bf741de8c78f1f50923f9118a5 OffsetDelta
            // 03350d0fe3ad12e8b20578ddb167a84aa5dac6d1 Tree
            // 0a84408db59739d3f98fbb1ca11af765386263b3 Blob
            // 0f9cf2009d25b5f387e6c92e9b26d4a2dc174a1f Commit

            _idxStream = File.OpenRead("Data\\pack-1.idx");
            _packStream = File.OpenRead("Data\\pack-1.pack");
            _packFile = new PackFileReader(_idxStream, _packStream);
        }

        public void Dispose()
        {
            _idxStream.Dispose();
            _packStream.Dispose();
        }

        [Fact]
        public void ObjectCount()
        {
            Assert.Equal(69u, _packFile.ObjectCount);
        }

        [Fact]
        public void TryReadEntry_returns_false_for_unkonwn_object_id()
        {
            Assert.False(_packFile.TryReadEntry(ObjectId.Zero, out _));
        }

        [Fact]
        public void TryReadEntry_succeeds_for_tree()
        {
            var objectId = ObjectId.Parse("03350d0fe3ad12e8b20578ddb167a84aa5dac6d1");
            Assert.True(_packFile.TryReadEntry(objectId, out var entry));
            Assert.Equal(PackFileEntryType.Tree, entry.Type);
            Assert.Equal(330, entry.Size);
            Assert.Equal(2892, entry.EntryOffset);
            Assert.Equal(2894, entry.DataOffset);
        }

        [Fact]
        public void TryReadEntry_succeeds_for_commit()
        {
            var objectId = ObjectId.Parse("0f9cf2009d25b5f387e6c92e9b26d4a2dc174a1f");
            Assert.True(_packFile.TryReadEntry(objectId, out var entry));
            Assert.Equal(PackFileEntryType.Commit, entry.Type);
            Assert.Equal(239, entry.Size);
            Assert.Equal(1411, entry.EntryOffset);
            Assert.Equal(1413, entry.DataOffset);
        }

        [Fact]
        public void TryReadEntry_succeeds_for_blob()
        {
            var objectId = ObjectId.Parse("0a84408db59739d3f98fbb1ca11af765386263b3");
            Assert.True(_packFile.TryReadEntry(objectId, out var entry));
            Assert.Equal(PackFileEntryType.Blob, entry.Type);
            Assert.Equal(857, entry.Size);
            Assert.Equal(3278, entry.EntryOffset);
            Assert.Equal(3280, entry.DataOffset);
        }

        [Fact]
        public void ReadObject_Tree()
        {
            var objectId = ObjectId.Parse("03350d0fe3ad12e8b20578ddb167a84aa5dac6d1");
            Assert.True(_packFile.TryReadEntry(objectId, out var entry));
            var obj = _packFile.ReadObject(entry);
            Assert.Equal(GitObjectType.Tree, obj.Type);
            Assert.Equal(330, obj.Bytes.Length);
            /*
            Binary encoded data including file names, modes, object IDs, object types

            $ git cat-file -p 0379e6bbd07a38d31d91c59c0eb81e31e9ca7ad7
            100644 blob 04cabd8ecf4bf0741569c1add9b93a8e9bedda61    Boing.csproj
            100644 blob 739e60c30d5dae2d5f749fa39006cc4c542094d7    Edge.cs
            040000 tree 0b52c9bcf98a303f230fff4e15274728b833fc0b    Forces
            100644 blob 34ef30277e35aee1750de7b0e2ecbe006580b32e    Graph.cs
            100644 blob 13792e31a1f4c8cea8b82c64dd40342f51b25552    IForce.cs
            100644 blob 60df32944eed6e78b5111fc24f53af21870c6f05    Node.cs
            100644 blob d1503553e54886b0acc80c0d8806ebb6cbc26c80    Physics.cs
            040000 tree 526915ab1671ffbd90faf459f7f8000089d7eca5    Properties
            100644 blob c928dbf111ee3adb050eb74d8ec91e92a7b9fb52    Vector2f.cs
            */
            var expected = new byte[]
            {
                49, 48, 48, 54, 52, 52, 32, 66, 111, 105, 110, 103, 46, 99, 115, 112, 114, 111, 106, 0, 4, 202, 189,
                142, 207, 75, 240, 116, 21, 105, 193, 173, 217, 185, 58, 142, 155, 237, 218, 97, 49, 48, 48, 54, 52, 52,
                32, 69, 100, 103, 101, 46, 99, 115, 0, 115, 158, 96, 195, 13, 93, 174, 45, 95, 116, 159, 163, 144, 6,
                204, 76, 84, 32, 148, 215, 52, 48, 48, 48, 48, 32, 70, 111, 114, 99, 101, 115, 0, 11, 82, 201, 188, 249,
                138, 48, 63, 35, 15, 255, 78, 21, 39, 71, 40, 184, 51, 252, 11, 49, 48, 48, 54, 52, 52, 32, 71, 114, 97,
                112, 104, 46, 99, 115, 0, 52, 239, 48, 39, 126, 53, 174, 225, 117, 13, 231, 176, 226, 236, 190, 0, 101,
                128, 179, 46, 49, 48, 48, 54, 52, 52, 32, 73, 70, 111, 114, 99, 101, 46, 99, 115, 0, 19, 121, 46, 49,
                161, 244, 200, 206, 168, 184, 44, 100, 221, 64, 52, 47, 81, 178, 85, 82, 49, 48, 48, 54, 52, 52, 32, 78,
                111, 100, 101, 46, 99, 115, 0, 96, 223, 50, 148, 78, 237, 110, 120, 181, 17, 31, 194, 79, 83, 175, 33,
                135, 12, 111, 5, 49, 48, 48, 54, 52, 52, 32, 80, 104, 121, 115, 105, 99, 115, 46, 99, 115, 0, 209, 80,
                53, 83, 229, 72, 134, 176, 172, 200, 12, 13, 136, 6, 235, 182, 203, 194, 108, 128, 52, 48, 48, 48, 48,
                32, 80, 114, 111, 112, 101, 114, 116, 105, 101, 115, 0, 82, 105, 21, 171, 22, 113, 255, 189, 144, 250,
                244, 89, 247, 248, 0, 0, 137, 215, 236, 165, 49, 48, 48, 54, 52, 52, 32, 86, 101, 99, 116, 111, 114, 50,
                102, 46, 99, 115, 0, 201, 40, 219, 241, 17, 238, 58, 219, 5, 14, 183, 77, 142, 201, 30, 146, 167, 185,
                251, 82
            };
            Assert.Equal(expected, obj.Bytes);
        }

        [Fact]
        public void ReadObject_Commit()
        {
            var objectId = ObjectId.Parse("0f9cf2009d25b5f387e6c92e9b26d4a2dc174a1f");
            Assert.True(_packFile.TryReadEntry(objectId, out var entry));
            var obj = _packFile.ReadObject(entry);
            Assert.Equal(GitObjectType.Commit, obj.Type);
            Assert.Equal(239, obj.Bytes.Length);

            /*
            tree 4da7194d1b9cd4bbded80fb7468f12122a6aaba2
            parent 7a68b0bd46c083f4bb1a66359dc89dc5e3c53428
            author Drew Noakes <git@drewnoakes.com> 1428795415 +0100
            committer Drew Noakes <git@drewnoakes.com> 1428795415 +0100

            Add KeepWithinBoundsForce.
            */

            var expected = new byte[]
            {
                116, 114, 101, 101, 32, 52, 100, 97, 55, 49, 57, 52, 100, 49, 98, 57, 99, 100, 52, 98, 98, 100, 101,
                100, 56, 48, 102, 98, 55, 52, 54, 56, 102, 49, 50, 49, 50, 50, 97, 54, 97, 97, 98, 97, 50, 10, 112, 97,
                114, 101, 110, 116, 32, 55, 97, 54, 56, 98, 48, 98, 100, 52, 54, 99, 48, 56, 51, 102, 52, 98, 98, 49,
                97, 54, 54, 51, 53, 57, 100, 99, 56, 57, 100, 99, 53, 101, 51, 99, 53, 51, 52, 50, 56, 10, 97, 117, 116,
                104, 111, 114, 32, 68, 114, 101, 119, 32, 78, 111, 97, 107, 101, 115, 32, 60, 103, 105, 116, 64, 100,
                114, 101, 119, 110, 111, 97, 107, 101, 115, 46, 99, 111, 109, 62, 32, 49, 52, 50, 56, 55, 57, 53, 52,
                49, 53, 32, 43, 48, 49, 48, 48, 10, 99, 111, 109, 109, 105, 116, 116, 101, 114, 32, 68, 114, 101, 119,
                32, 78, 111, 97, 107, 101, 115, 32, 60, 103, 105, 116, 64, 100, 114, 101, 119, 110, 111, 97, 107, 101,
                115, 46, 99, 111, 109, 62, 32, 49, 52, 50, 56, 55, 57, 53, 52, 49, 53, 32, 43, 48, 49, 48, 48, 10, 10,
                65, 100, 100, 32, 75, 101, 101, 112, 87, 105, 116, 104, 105, 110, 66, 111, 117, 110, 100, 115, 70, 111,
                114, 99, 101, 46, 10
            };
            Assert.Equal(expected, obj.Bytes);
        }

        [Fact]
        public void ReadObject_Blob()
        {
            var objectId = ObjectId.Parse("0a84408db59739d3f98fbb1ca11af765386263b3");
            Assert.True(_packFile.TryReadEntry(objectId, out var entry));
            var obj = _packFile.ReadObject(entry);
            Assert.Equal(GitObjectType.Blob, obj.Type);
            Assert.Equal(857, obj.Bytes.Length);

            /*
            <?xml version="1.0"?>
            <package>
              <metadata>
                <id>Boing</id>
                <version>0.1.0</version>
                <title>Boing - Simple physics sandbox for .NET</title>
                <authors>Drew Noakes, Krzysztof Dul</authors>
                <owners>Drew Noakes, Krzysztof Dul</owners>
                <licenseUrl>https://www.apache.org/licenses/LICENSE-2.0.html</licenseUrl>
                <projectUrl>https://github.com/drewnoakes/boing</projectUrl>
                <!--<iconUrl>http://ICON_URL_HERE_OR_DELETE_THIS_LINE</iconUrl>-->
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Simple physics sandbox for .NET</description>
                <releaseNotes>Initial release.</releaseNotes>
                <copyright>Copyright Drew Noakes, Krzysztof Dul 2015</copyright>
                <tags>physics simulation</tags>
              </metadata>
              <files>
                <file src="Boing\bin\Release\Boing.dll" target="lib\net20" />
              </files>
            </package>
            */

            var expected = new byte[]
            {
                60, 63, 120, 109, 108, 32, 118, 101, 114, 115, 105, 111, 110, 61, 34, 49, 46, 48, 34, 63, 62, 10, 60,
                112, 97, 99, 107, 97, 103, 101, 62, 10, 32, 32, 60, 109, 101, 116, 97, 100, 97, 116, 97, 62, 10, 32, 32,
                32, 32, 60, 105, 100, 62, 66, 111, 105, 110, 103, 60, 47, 105, 100, 62, 10, 32, 32, 32, 32, 60, 118,
                101, 114, 115, 105, 111, 110, 62, 48, 46, 49, 46, 48, 60, 47, 118, 101, 114, 115, 105, 111, 110, 62, 10,
                32, 32, 32, 32, 60, 116, 105, 116, 108, 101, 62, 66, 111, 105, 110, 103, 32, 45, 32, 83, 105, 109, 112,
                108, 101, 32, 112, 104, 121, 115, 105, 99, 115, 32, 115, 97, 110, 100, 98, 111, 120, 32, 102, 111, 114,
                32, 46, 78, 69, 84, 60, 47, 116, 105, 116, 108, 101, 62, 10, 32, 32, 32, 32, 60, 97, 117, 116, 104, 111,
                114, 115, 62, 68, 114, 101, 119, 32, 78, 111, 97, 107, 101, 115, 44, 32, 75, 114, 122, 121, 115, 122,
                116, 111, 102, 32, 68, 117, 108, 60, 47, 97, 117, 116, 104, 111, 114, 115, 62, 10, 32, 32, 32, 32, 60,
                111, 119, 110, 101, 114, 115, 62, 68, 114, 101, 119, 32, 78, 111, 97, 107, 101, 115, 44, 32, 75, 114,
                122, 121, 115, 122, 116, 111, 102, 32, 68, 117, 108, 60, 47, 111, 119, 110, 101, 114, 115, 62, 10, 32,
                32, 32, 32, 60, 108, 105, 99, 101, 110, 115, 101, 85, 114, 108, 62, 104, 116, 116, 112, 115, 58, 47, 47,
                119, 119, 119, 46, 97, 112, 97, 99, 104, 101, 46, 111, 114, 103, 47, 108, 105, 99, 101, 110, 115, 101,
                115, 47, 76, 73, 67, 69, 78, 83, 69, 45, 50, 46, 48, 46, 104, 116, 109, 108, 60, 47, 108, 105, 99, 101,
                110, 115, 101, 85, 114, 108, 62, 10, 32, 32, 32, 32, 60, 112, 114, 111, 106, 101, 99, 116, 85, 114, 108,
                62, 104, 116, 116, 112, 115, 58, 47, 47, 103, 105, 116, 104, 117, 98, 46, 99, 111, 109, 47, 100, 114,
                101, 119, 110, 111, 97, 107, 101, 115, 47, 98, 111, 105, 110, 103, 60, 47, 112, 114, 111, 106, 101, 99,
                116, 85, 114, 108, 62, 10, 32, 32, 32, 32, 60, 33, 45, 45, 60, 105, 99, 111, 110, 85, 114, 108, 62, 104,
                116, 116, 112, 58, 47, 47, 73, 67, 79, 78, 95, 85, 82, 76, 95, 72, 69, 82, 69, 95, 79, 82, 95, 68, 69,
                76, 69, 84, 69, 95, 84, 72, 73, 83, 95, 76, 73, 78, 69, 60, 47, 105, 99, 111, 110, 85, 114, 108, 62, 45,
                45, 62, 10, 32, 32, 32, 32, 60, 114, 101, 113, 117, 105, 114, 101, 76, 105, 99, 101, 110, 115, 101, 65,
                99, 99, 101, 112, 116, 97, 110, 99, 101, 62, 102, 97, 108, 115, 101, 60, 47, 114, 101, 113, 117, 105,
                114, 101, 76, 105, 99, 101, 110, 115, 101, 65, 99, 99, 101, 112, 116, 97, 110, 99, 101, 62, 10, 32, 32,
                32, 32, 60, 100, 101, 115, 99, 114, 105, 112, 116, 105, 111, 110, 62, 83, 105, 109, 112, 108, 101, 32,
                112, 104, 121, 115, 105, 99, 115, 32, 115, 97, 110, 100, 98, 111, 120, 32, 102, 111, 114, 32, 46, 78,
                69, 84, 60, 47, 100, 101, 115, 99, 114, 105, 112, 116, 105, 111, 110, 62, 10, 32, 32, 32, 32, 60, 114,
                101, 108, 101, 97, 115, 101, 78, 111, 116, 101, 115, 62, 73, 110, 105, 116, 105, 97, 108, 32, 114, 101,
                108, 101, 97, 115, 101, 46, 60, 47, 114, 101, 108, 101, 97, 115, 101, 78, 111, 116, 101, 115, 62, 10,
                32, 32, 32, 32, 60, 99, 111, 112, 121, 114, 105, 103, 104, 116, 62, 67, 111, 112, 121, 114, 105, 103,
                104, 116, 32, 68, 114, 101, 119, 32, 78, 111, 97, 107, 101, 115, 44, 32, 75, 114, 122, 121, 115, 122,
                116, 111, 102, 32, 68, 117, 108, 32, 50, 48, 49, 53, 60, 47, 99, 111, 112, 121, 114, 105, 103, 104, 116,
                62, 10, 32, 32, 32, 32, 60, 116, 97, 103, 115, 62, 112, 104, 121, 115, 105, 99, 115, 32, 115, 105, 109,
                117, 108, 97, 116, 105, 111, 110, 60, 47, 116, 97, 103, 115, 62, 10, 32, 32, 60, 47, 109, 101, 116, 97,
                100, 97, 116, 97, 62, 10, 32, 32, 60, 102, 105, 108, 101, 115, 62, 10, 32, 32, 32, 32, 60, 102, 105,
                108, 101, 32, 115, 114, 99, 61, 34, 66, 111, 105, 110, 103, 92, 98, 105, 110, 92, 82, 101, 108, 101, 97,
                115, 101, 92, 66, 111, 105, 110, 103, 46, 100, 108, 108, 34, 32, 116, 97, 114, 103, 101, 116, 61, 34,
                108, 105, 98, 92, 110, 101, 116, 50, 48, 34, 32, 47, 62, 10, 32, 32, 60, 47, 102, 105, 108, 101, 115,
                62, 10, 60, 47, 112, 97, 99, 107, 97, 103, 101, 62, 10
            };
            Assert.Equal(expected, obj.Bytes);
        }

        [Fact]
        public void ReadObject_OffsetDelta()
        {
            var objectId = ObjectId.Parse("0251b616d8c2e7bf741de8c78f1f50923f9118a5");

            Assert.True(_packFile.TryReadEntry(objectId, out var entry));

            var obj = _packFile.ReadObject(entry);

            Assert.Equal(GitObjectType.Blob, obj.Type);
            Assert.Equal(1496, obj.Bytes.Length);

            _output.WriteLine(obj.GetString());
        }

        [Fact]
        public void EntryLookupPerf()
        {
            var objectId = ObjectId.Parse("0251b616d8c2e7bf741de8c78f1f50923f9118a5");

            var sw = Stopwatch.StartNew();

            const int loopCount = 10_000;

            for (var i = 0; i < loopCount; i++)
                _packFile.TryReadEntry(objectId, out _);

            var millis = sw.Elapsed.TotalMilliseconds;

            _output.WriteLine($"{loopCount:#,##0} lookups in {millis} ms ({1000*millis/loopCount:0.#} µs per lookup)");
        }

        [Fact]
        public void DumpObjects()
        {
            foreach (var id in _packFile.ObjectIds.ToList())
            {
                _packFile.TryReadEntry(id, out var e);
                _output.WriteLine($"{id} {e.Type} ({e.Size} bytes)");
            }
        }

        [Fact]
        public void DumpEntries()
        {
            foreach (var (id, entry) in _packFile.Entries.ToList())
                _output.WriteLine($"{id} {entry.Type} ({entry.Size} bytes)");
        }
    }
}
