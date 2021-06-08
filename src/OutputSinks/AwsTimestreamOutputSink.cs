using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.TimestreamWrite;
using Amazon.TimestreamWrite.Model;
using PollQT.DataTypes;
using Serilog;

namespace PollQT.OutputSinks
{
    class AwsTimestreamOutputSink : IOutputSink, IDisposable {
        private readonly ILogger log;

        private readonly string dbName = Environment.GetEnvironmentVariable("TS_DBNAME") ?? throw new ArgumentNullException();
        private readonly string tblName = Environment.GetEnvironmentVariable("TS_TBLNAME") ?? throw new ArgumentNullException();
        private readonly AmazonTimestreamWriteClient writeClient;
        private readonly ConcurrentQueue<PollResult> writeQueue = new ConcurrentQueue<PollResult>();
        private readonly ConcurrentDictionary<string, PollResult> prevResults = new ConcurrentDictionary<string, PollResult>();

        private readonly uint batchIntervalMs = 1000 * uint.Parse(Environment.GetEnvironmentVariable("TS_INTERVAL_SECS") ?? "600");

        private readonly Timer batchTimer;

        public AwsTimestreamOutputSink(Context context) {
            log = context.Logger.ForContext<AwsTimestreamOutputSink>();

            var writeClientConfig = new AmazonTimestreamWriteConfig
            {
                RegionEndpoint = RegionEndpoint.USEast1,
                Timeout = TimeSpan.FromSeconds(20),
                MaxErrorRetry = 10
            };
            writeClient = new AmazonTimestreamWriteClient(writeClientConfig);
            batchTimer = new Timer(async (object? s) => await BatchTimerCallback(s), null, batchIntervalMs, batchIntervalMs);

            log.Information("Starting AWS Timestream output sink, writes every {BatchInterval}s with config {@config}", batchIntervalMs/1000, writeClientConfig);
        }


        private List<WriteRecordsRequest> MakePositionRecords(PollResult pollResult) {
            var writeRecordRequests = new List<WriteRecordsRequest>();
            var accountDimension = new Dimension { Name = "act", Value = $"{pollResult.Account.Type}-{pollResult.Account.Number}" };

            foreach (var positionResult in pollResult.Positions) {
                var positionDimensions = new List<Dimension>
                    {
                        accountDimension,
                        new Dimension { Name = "sym", Value = $"{positionResult.Symbol}" }
                    };

                var commonAttributes = new Record
                {
                    Dimensions = positionDimensions,
                    MeasureValueType = MeasureValueType.DOUBLE,
                    Time = pollResult.Timestamp.ToUnixTimeMilliseconds().ToString()
                };

                var currentValue = new Record
                {
                    MeasureName = "cmv",
                    MeasureValue = positionResult.CurrentMarketValue.ToString()
                };

                var totalCost = new Record
                {
                    MeasureName = "cost",
                    MeasureValue = positionResult.TotalCost.ToString()
                };

                writeRecordRequests.Add(new WriteRecordsRequest
                {
                    DatabaseName = dbName,
                    TableName = tblName,
                    Records = new List<Record> { currentValue, totalCost },
                    CommonAttributes = commonAttributes
                });
            }
            return writeRecordRequests;
        }

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

        private async Task WriteRecords(WriteRecordsRequest writeRecordsRequest) {
            try {
                WriteRecordsResponse response = await writeClient.WriteRecordsAsync(writeRecordsRequest);
                log.Information("Success: {@resp}", response);
            } catch (RejectedRecordsException e) {
                log.Error(e, "Write records had rejected records");
                foreach (RejectedRecord rr in e.RejectedRecords) {
                    log.Error("RecordIndex {RecordIndex}: {Reason}", rr.RecordIndex, rr.Reason);
                }
                log.Error("{SuccessCount} other records were written successfully", writeRecordsRequest.Records.Count - e.RejectedRecords.Count);
            } catch (Exception e) {
                log.Error(e, "Failure writing records");
            }
        }

        private async Task BatchTimerCallback(object? state) {
            log.Information("Batch write started - processing queue");
            var batchPollResults = new List<PollResult>();
            while (writeQueue.TryDequeue(out var pollResult)) {
                if (ShouldWrite(pollResult)) {
                    batchPollResults.Add(pollResult);
                } else {
                    log.Information("Skipping {AccountType}-{AccountNumber} {timestamp} results",
                        pollResult.Account.Type, pollResult.Account.Number, pollResult.Timestamp);
                }
            }
            var writeRecordRequests = new List<WriteRecordsRequest>();
            batchPollResults.ForEach(pollResult => writeRecordRequests.AddRange(MakePositionRecords(pollResult)));

            var requestTasks = writeRecordRequests.Select(r => WriteRecords(r));
            await Task.WhenAll(requestTasks);
        }

        private void Enqueue(PollResult pollResult) {
            log.Information("Enqueuing {AccountType}-{AccountNumber} {timestamp} results",
                    pollResult.Account.Type, pollResult.Account.Number, pollResult.Timestamp);
            writeQueue.Enqueue(pollResult);
        }

        public async Task NewEvent(List<PollResult> pollResults) {   
            pollResults.ForEach(Enqueue);
            await Task.CompletedTask;
        }

        public void Dispose() => batchTimer.Dispose();
    }
}
