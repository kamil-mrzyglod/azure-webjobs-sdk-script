﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Loggers;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.WindowsAzure.Storage;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics
{
    // Adapter for capturing SDK events and logging them to tables.
    internal class FastLogger : IAsyncCollector<FunctionInstanceLogEntry>
    {
        private readonly ILogWriter _writer;

        public FastLogger(string hostName, string accountConnectionString, TraceWriter trace)
        {
            if (trace == null)
            {
                throw new ArgumentNullException(nameof(trace));
            }

            CloudStorageAccount account = CloudStorageAccount.Parse(accountConnectionString);
            var client = account.CreateCloudTableClient();
            var tableProvider = LogFactory.NewLogTableProvider(client);

            string containerName = Environment.MachineName;
            this._writer = LogFactory.NewWriter(hostName, containerName, tableProvider, (e) => OnException(e, trace));
        }

        public async Task AddAsync(FunctionInstanceLogEntry item, CancellationToken cancellationToken = default(CancellationToken))
        {
            await _writer.AddAsync(new FunctionInstanceLogItem
            {
                FunctionInstanceId = item.FunctionInstanceId,
                FunctionName = Utility.GetFunctionShortName(item.FunctionName),
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                TriggerReason = item.TriggerReason,
                Arguments = item.Arguments,
                ErrorDetails = item.ErrorDetails,
                LogOutput = item.LogOutput,
                ParentId = item.ParentId
            });
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _writer.FlushAsync();
        }

        public static void OnException(Exception exception, TraceWriter trace)
        {
            string errorString = $"Error writing logs to table storage: {exception.ToString()}";
            trace.Error(errorString, exception);
        }
    }
}
