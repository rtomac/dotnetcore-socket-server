using log4net;
using System;
using System.IO;
using System.Threading;

namespace Server
{
    public class StatusReporter : IDisposable
    {
        public int TotalUnique { get; private set; }
        public int TotalDuplicates { get; private set; }
        public int IncrementalUnique { get; private set; }
        public int IncrementalDuplicates { get; private set; }

        private static ILog _log = LogManager.GetLogger(typeof(StatusReporter));

        private readonly ReaderWriterLockSlim _lock;
        private readonly ManualResetEventSlim _stopSignal;

        public StatusReporter()
        {
            _lock = new ReaderWriterLockSlim();
            _stopSignal = new ManualResetEventSlim();
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

        public void Dispose()
        {
            Stop();
        }
    }
}
