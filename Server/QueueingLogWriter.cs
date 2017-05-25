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
        private readonly ManualResetEventSlim _itemsInQueue;

        public QueueingLogWriter(TextWriter writer)
        {
            _writer = writer;
            _queue = new Queue<int>();
            _deduper = new HashSet<int>();
            _lock = new object();
            _stopSignal = new ManualResetEventSlim();
            _itemsInQueue = new ManualResetEventSlim();

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
                _itemsInQueue.Set();
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
                while (!_stopSignal.IsSet)
                {
                    _itemsInQueue.Wait();

                    _writer.WriteLine(_queue.Dequeue());

                    lock (_lock)
                    {
                        if (_queue.Count == 0)
                        {
                            _itemsInQueue.Reset();
                            _writer.Flush();
                        }
                    }
                }
            }));
            thread.Start();
        }

        private void StopWatchingQueue()
        {
            _writer.Flush();
            _stopSignal.Set();
        }
    }
}
