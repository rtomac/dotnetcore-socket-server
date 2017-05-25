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

        private void WriteToFile(int value)
        {
            var str = value.ToString();
            if (str.Length < 9)
            {
                str = String.Concat(new String('0', 9 - str.Length), str);
            }
            _writer.WriteLine(str);
        }

        private void StartWatchingQueue()
        {
            _stopSignal.Reset();
            var thread = new Thread(new ThreadStart(() =>
            {
                while (!_stopSignal.IsSet)
                {
                    _itemsInQueue.Wait();

                    WriteToFile(_queue.Dequeue());

                    lock (_lock)
                    {
                        if (_queue.Count == 0)
                        {
                            _itemsInQueue.Reset();
                        }
                    }
                }
            }));
            thread.Start();
        }

        private void StopWatchingQueue()
        {
            _stopSignal.Set();
        }
    }
}
