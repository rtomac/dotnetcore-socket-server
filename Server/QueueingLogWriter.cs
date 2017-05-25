using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Server
{
    public class QueueingLogWriter : IDisposable
    {
        private readonly TextWriter _writer;
        private readonly Queue<int> _queue;
        private readonly HashSet<int> _deduper;
        private readonly object _lock;
        private readonly ManualResetEventSlim _stopSignal;
        private readonly ManualResetEventSlim _workerSignal;

        public QueueingLogWriter(TextWriter writer)
        {
            _writer = writer;
            _queue = new Queue<int>();
            _deduper = new HashSet<int>();
            _lock = new object();
            _stopSignal = new ManualResetEventSlim();
            _workerSignal = new ManualResetEventSlim();

            StartWatchingQueue();
        }

        public bool WriteUnique(int value)
        {
            lock (_lock)
            {
                if (!_deduper.Add(value))
                {
                    return false;
                }
                _queue.Enqueue(value);
                _workerSignal.Set();
                return true;
            }
        }

        public void Dispose()
        {
            StopWatchingQueue();
        }

        private void StartWatchingQueue()
        {
            _stopSignal.Reset();
            var thread = new Thread(new ThreadStart(() =>
            {
                int value;
                bool flush;
                var total = 0;
                while (!_stopSignal.IsSet)
                {
                    _workerSignal.Wait();
                    if (_stopSignal.IsSet) break;

                    lock (_lock)
                    {
                        total++;
                        value = _queue.Dequeue();
                        flush = (total % 100000) == 0;

                        if (_queue.Count == 0)
                        {
                            _workerSignal.Reset();
                            flush = true;
                        }
                    }

                    _writer.WriteLine(value);

                    if (flush)
                    {
                        _writer.Flush();
                    }
                }
            }));
            thread.Start();
        }

        private void StopWatchingQueue()
        {
            _writer.Flush();
            _workerSignal.Set();
            _stopSignal.Set();
        }
    }
}
