using System;

namespace CookedOut.Domain
{
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }

    public interface IRandomSource
    {
        int NextInt(int minInclusive, int maxExclusive);
    }

    public interface IAnalyticsSink
    {
        void Track(string eventId, string payload);
    }

    public interface ISaveStore
    {
        void Save(string slotId, string serializedState);
        bool TryLoad(string slotId, out string serializedState);
    }

    public interface INetworkSessionGateway
    {
        bool IsAvailable { get; }
        void SubmitResult(string serializedResult);
    }
}
