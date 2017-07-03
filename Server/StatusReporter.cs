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
    /// Uses <c>lock</c> to synchronize access to the aggregate
    /// numbers, since [many] other threads processing data from
    /// connections will be recording new numbers.
    /// 
    /// <c>_stopSignal</c> reset event used to release background thread
    /// and stop writing status reports when <c>Stop</c> is called.
    /// </remarks>
    public class StatusReporter
    {
        public int TotalUnique => _totalUnique;
        public int TotalDuplicates => _totalDuplicates;
        public int IncrementalUnique => _incrementalUnique;
        public int IncrementalDuplicates => _incrementalDuplicates;

        private int _totalUnique;
        private int _totalDuplicates;
        private int _incrementalUnique;
        private int _incrementalDuplicates;
        private readonly object _lock;
        private readonly ManualResetEventSlim _stopSignal;

        private static ILog _log = LogManager.GetLogger(typeof(StatusReporter));

        public StatusReporter()
        {
            _lock = new object();
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
            lock (_lock)
            {
                _incrementalUnique++;
                _totalUnique++;
            }
        }

        public void RecordDuplicate()
        {
            lock (_lock)
            {
                _incrementalDuplicates++;
                _totalDuplicates++;
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
            lock (_lock)
            {
                incrementalUnique = _incrementalUnique;
                incrementalDups = _incrementalDuplicates;
                totalUnique = _totalUnique;

                _incrementalUnique = _incrementalDuplicates = 0;
            }

            writer.Write($"Received {incrementalUnique} unique numbers, {incrementalDups} duplicates. Unique total: {totalUnique}");
        }
    }
}
