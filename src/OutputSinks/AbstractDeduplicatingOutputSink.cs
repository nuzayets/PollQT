using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PollQT.DataTypes;
using Serilog;

namespace PollQT.OutputSinks
{
    internal abstract class AbstractDeduplicatingOutputSink : IOutputSink
    {
        protected abstract ILogger Log { get; }

        private readonly ConcurrentDictionary<string, PollResult> prevResults = new();

        private bool ShouldWrite(PollResult pollResult) {
            // don't write entries with the exact same data as before
            var entry = prevResults.GetOrAdd(pollResult.Account.Number, pollResult);
            if (pollResult.Positions.SequenceEqual(entry.Positions)
                && pollResult.Balance.Equals(entry.Balance)) {
                // if the data match & timestamp match, do write - GetOrAdd did Add
                // if the data match & timestamp don't match, don't write - GetOrAdd did Get of the same
                return pollResult.Timestamp == entry.Timestamp;
            } else {
                // always write if the data don't match
                _ = prevResults.TryUpdate(pollResult.Account.Number, pollResult, entry);
                return true;
            }
        }
        public async Task NewEvent(List<PollResult> pollResults) {
            foreach (var pollResult in pollResults) {
                if (!ShouldWrite(pollResult)) {
                    Log.Information("Skipping {AccountType}-{AccountNumber} at {Timestamp} due to it being identical to the last result",
                        pollResult.Account.Type, pollResult.Account.Number, pollResult.Timestamp);
                } else {
                    await Write(pollResult);
                }
            }
        }

        protected abstract Task Write(PollResult pollResult);
    }
}
