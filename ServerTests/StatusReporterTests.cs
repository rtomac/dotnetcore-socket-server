using Server;
using System.IO;
using Xunit;

namespace ServerTests
{
    public class StatusReporterTests
    {
        [Fact]
        public void TestRecordUnique()
        {
            var reporter = new StatusReporter();

            Assert.Equal(0, reporter.IncrementalUnique);
            Assert.Equal(0, reporter.TotalUnique);

            for (var i = 0; i < 10; i++)
            {
                reporter.RecordUnique();

                Assert.Equal(i + 1, reporter.IncrementalUnique);
                Assert.Equal(i + 1, reporter.TotalUnique);
            }
        }

        [Fact]
        public void TestRecordDuplicates()
        {
            var reporter = new StatusReporter();

            Assert.Equal(0, reporter.IncrementalDuplicates);
            Assert.Equal(0, reporter.TotalDuplicates);

            for (var i = 0; i < 10; i++)
            {
                reporter.RecordDuplicate();

                Assert.Equal(i + 1, reporter.IncrementalDuplicates);
                Assert.Equal(i + 1, reporter.TotalDuplicates);
            }
        }

        [Fact]
        public void TestReport()
        {
            var reporter = new StatusReporter();

            for (var i = 0; i < 32; i++)
                reporter.RecordUnique();

            for (var i = 0; i < 4; i++)
                reporter.RecordDuplicate();

            using (var writer = new StringWriter())
            {
                reporter.Report(writer);
                Assert.Equal(
                    "Received 32 unique numbers, 4 duplicates. Unique total: 32",
                    writer.ToString());
            }

            Assert.Equal(32, reporter.TotalUnique);
            Assert.Equal(4, reporter.TotalDuplicates);
            Assert.Equal(0, reporter.IncrementalUnique);
            Assert.Equal(0, reporter.IncrementalDuplicates);

            for (var i = 0; i < 31; i++)
                reporter.RecordUnique();

            for (var i = 0; i < 3; i++)
                reporter.RecordDuplicate();

            using (var writer = new StringWriter())
            {
                reporter.Report(writer);
                Assert.Equal(
                    "Received 31 unique numbers, 3 duplicates. Unique total: 63",
                    writer.ToString());
            }

            Assert.Equal(63, reporter.TotalUnique);
            Assert.Equal(7, reporter.TotalDuplicates);
            Assert.Equal(0, reporter.IncrementalUnique);
            Assert.Equal(0, reporter.IncrementalDuplicates);
        }
    }
}
