using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Server
{
    public class QueueingLogWriter
    {
        private readonly TextWriter _writer;
        private readonly Queue<int> _queue;
        private readonly HashSet<int> _deduper;
        private readonly object _syncRoot;
        private readonly ManualResetEventSlim _stopSignal;
        private readonly ManualResetEventSlim _itemsInQueue;

        public QueueingLogWriter(TextWriter writer)
        {
            _writer = writer;
            _queue = new Queue<int>();
            _deduper = new HashSet<int>();
            _syncRoot = new object();
            _stopSignal = new ManualResetEventSlim();
            _itemsInQueue = new ManualResetEventSlim();
        }

        public bool WriteUnique(int value)
        {
            lock (_syncRoot)
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

        public void Start()
        {
            var thread = new Thread(new ThreadStart(() =>
            {
                string value;
                while (!_stopSignal.IsSet)
                {
                    _itemsInQueue.Wait();

                    value = _queue.Dequeue().ToString();
                    if (value.Length < 9)
                    {
                        value = String.Concat(new String('0', 9 - value.Length), value);
                    }
                    _writer.WriteLine(value);

                    lock (_syncRoot)
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

        public void Stop()
        {
            _stopSignal.Set();
        }
    }
}
