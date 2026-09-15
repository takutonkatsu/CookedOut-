using System;
using System.Collections.Generic;
using CookedOut.Domain;

namespace CookedOut.Infrastructure
{
    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    public sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset value)
        {
            UtcNow = value;
        }

        public DateTimeOffset UtcNow { get; }
    }

    public sealed class SeededRandomSource : IRandomSource
    {
        private uint _state;

        public SeededRandomSource(uint seed)
        {
            _state = seed == 0 ? 0x6d2b79f5u : seed;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            var value = NextUInt();
            return minInclusive + (int)(value % (uint)(maxExclusive - minInclusive));
        }

        private uint NextUInt()
        {
            var x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }
    }

    public sealed class NullAnalyticsSink : IAnalyticsSink
    {
        public void Track(string eventId, string payload)
        {
        }
    }

    public sealed class InMemorySaveStore : ISaveStore
    {
        private readonly Dictionary<string, string> _slots = new Dictionary<string, string>();

        public void Save(string slotId, string serializedState)
        {
            _slots[slotId] = serializedState;
        }

        public bool TryLoad(string slotId, out string serializedState)
        {
            return _slots.TryGetValue(slotId, out serializedState);
        }
    }

    public sealed class OfflineNetworkSessionGateway : INetworkSessionGateway
    {
        public bool IsAvailable => false;

        public void SubmitResult(string serializedResult)
        {
            throw new InvalidOperationException("Network session gateway is not configured for the vertical slice.");
        }
    }
}
