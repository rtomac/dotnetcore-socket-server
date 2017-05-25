using Server;
using Xunit;

namespace ServerTests
{
    public class DeduplicatorTests
    {
        [Fact]
        public void TestAdd()
        {
            var deduper = new Deduplicator();

            for (var i = 0; i < 100; i++)
            {
                Assert.True(deduper.IsUnique(i));
            }

            for (var i = 0; i < 100; i++)
            {
                Assert.False(deduper.IsUnique(i));
            }
        }
    }
}
