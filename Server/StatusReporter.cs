using log4net;
using System.IO;
using System.Threading;

namespace Server
{
    /// <summary>
    /// Keeps track of aggregate statistics and periodically writes
    /// status report to the output stream.
    /// </summary>
    /// <remarks>
    /// Creates a background thread to write status reports to the console
    /// on a specified interval.
    /// 
    /// Uses reader/writer lock to synchronize access to the aggregate
    /// numbers, since other threads processing data from connections will
    /// be recording new numbers.
    /// 
    /// <c>_stopSignal</c> reset event used to release background thread
    /// and stop writing status reports when <c>Stop</c> is called.
    /// </remarks>
    public class StatusReporter
    {
        public int TotalUnique { get; private set; }
        public int TotalDuplicates { get; private set; }
        public int IncrementalUnique { get; private set; }
        public int IncrementalDuplicates { get; private set; }

        private readonly ReaderWriterLockSlim _lock;
        private readonly ManualResetEventSlim _stopSignal;

        private static ILog _log = LogManager.GetLogger(typeof(StatusReporter));

        public StatusReporter()
        {
            _lock = new ReaderWriterLockSlim();
            _stopSignal = new ManualResetEventSlim();
        }

        public void Start(int reportingInterval)
        {
            _stopSignal.Reset();
            var thread = new Thread(new ThreadStart(() =>
            {
                while (!_stopSignal.IsSet)
                {
                    _stopSignal.Wait(reportingInterval * 1000);
                    if (_stopSignal.IsSet)
                    {
                        break;
                    }
                    Report();
                }
            }));
            thread.Start();
        }

        public void Stop()
        {
            _stopSignal.Set();
        }

        public void RecordUnique()
        {
            _lock.EnterReadLock();
            try
            {
                IncrementalUnique += 1;
                TotalUnique += 1;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void RecordDuplicate()
        {
            _lock.EnterReadLock();
            try
            {
                IncrementalDuplicates += 1;
                TotalDuplicates += 1;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Report()
        {
            using (var writer = new StringWriter())
            {
                Report(writer);
                _log.Info(writer.ToString());
            }

        }
        public void Report(TextWriter writer)
        {
            int incrementalUnique, incrementalDups, totalUnique;

            // Dereference the numbers we need, and write them afterword,
            // to minimize the amount of time we need to have a write lock
            // and be blocking other processing threads that are reporting status.
            _lock.EnterWriteLock();
            try
            {
                incrementalUnique = IncrementalUnique;
                incrementalDups = IncrementalDuplicates;
                totalUnique = TotalUnique;

                IncrementalUnique = IncrementalDuplicates = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            writer.Write($"Received {incrementalUnique} unique numbers, {incrementalDups} duplicates. Unique total: {totalUnique}");
        }
    }
}
