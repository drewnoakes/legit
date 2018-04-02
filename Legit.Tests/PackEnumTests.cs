using System;
using Xunit;

namespace Legit
{
    public sealed class PackEnumTests
    {
        [Fact]
        public void GitObjectType_maps_to_PackFileEntryType()
        {
            var objectTypes = (GitObjectType[])Enum.GetValues(typeof(GitObjectType));

            foreach (var objectType in objectTypes)
            {
                var entryType = (PackFileEntryType)objectType;

                Assert.Equal(objectType.ToString(), entryType.ToString());
            }
        }
    }
}
