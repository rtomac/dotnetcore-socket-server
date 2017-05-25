using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Server
{
    /// <summary>
    /// Writes numeric values to the specified <see cref="TextWriter"/>.
    /// Does this by managing an in-memory queue of values to be written,
    /// and processing that queue on a background worker thread. De-duplicates
    /// the values, such that only unique values are written to the stream.
    /// </summary>
    /// <remarks>
    /// Queueing design here intended to prevent file I/O from blocking
    /// calls to <c>WriteUnique</c>. Processing threads will be
    /// calling that as they are reading/processing data from network
    /// connections, and we don't want to slow them down.
    /// 
    /// This class has a few shared resources that it must synchronize:
    /// - The hash set used to enforce uniqueness of values we write.
    /// - The underlying queue.
    /// 
    /// Uses manual reset event (<c>_workerSignal</c>) to let the worker
    /// thread rest when there are no items on the queue. When new items
    /// are added, it gets signaled and picks back up again.
    /// </remarks>
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
                // If value is unique, add to queue and return quickly.
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
            // Create background thread to watch and process queue.
            _stopSignal.Reset();
            var thread = new Thread(new ThreadStart(() =>
            {
                int value;
                bool flush;
                var total = 0;
                while (!_stopSignal.IsSet) // Allows us to stop processing if writer is being disabled.
                {
                    _workerSignal.Wait(); // Block here until there is work to do (values to write).
                    if (_stopSignal.IsSet) break; // Check again. May have changed by now (see StopWatchingQueue).

                    // Synchronize access just to the queue here. Get value as quickly
                    // as possible and release lock. Write to file afterword.
                    lock (_lock)
                    {
                        total++;
                        value = _queue.Dequeue();
                        flush = (total % 100000) == 0; // We'll force a flush every 10k values for good measure.

                        if (_queue.Count == 0)
                        {
                            // If there's no items left in queue, we can return worker to resting state.
                            _workerSignal.Reset();
                            flush = true; // Also might as well force flush if we've written all values.
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
