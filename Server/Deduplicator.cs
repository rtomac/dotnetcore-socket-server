using System.Collections.Generic;

namespace Server
{
    public class Deduplicator
    {
        private readonly HashSet<int> _cache;
        private readonly object _syncRoot;

        public Deduplicator()
        {
            _cache = new HashSet<int>();
            _syncRoot = new object();
        }

        public bool IsUnique(int value)
        {
            lock (_syncRoot)
            {
                return _cache.Add(value);
            }
        }
    }
}
