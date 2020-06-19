/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.TestHelpers
{
    public static class RunCommandHelper
    {
        public static BsonDocument RunCommandAndRetryIfRequired(
            IMongoDatabase database,
            BsonDocument command,
            IClientSessionHandle clientSession = null,
            ReadPreference readPreference = null,
            CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(command, nameof(command));
            Ensure.IsNotNull(database, nameof(database));

            var commandName = command.GetElement(0).Name;

            switch (commandName)
            {
                case "replSetStepDown": return RunReplicaSetStepDown(database, command, clientSession, readPreference, cancellationToken);
                default:
                    return RunCommand(clientSession, database, command, readPreference, cancellationToken);
            }
        }

        public static async Task<BsonDocument> RunCommandAndRetryIfRequiredAsync(
            IMongoDatabase database,
            BsonDocument command,
            IClientSessionHandle clientSession = null,
            ReadPreference readPreference = null,
            CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(command, nameof(command));
            Ensure.IsNotNull(database, nameof(database));

            var commandName = command.GetElement(0).Name;

            switch (commandName)
            {
                case "replSetStepDown": return await RunReplicaSetStepDownAsync(database, command, clientSession, readPreference, cancellationToken).ConfigureAwait(false);
                default:
                    return await RunCommandAsync(clientSession, database, command, readPreference, cancellationToken).ConfigureAwait(false);
            }
        }

        public static BsonDocument RunReplicaSetStepDown(
            IMongoDatabase database,
            BsonDocument command,
            IClientSessionHandle clientSession = null,
            ReadPreference readPreference = null,
            CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(database, nameof(database));
            Ensure.IsNotNull(command, nameof(command));
            Ensure.That(command.TryGetValue("replSetStepDown", out _), "The command must be replSetStepDown.");

            BsonDocument replSetStepDownResult = null;
            using (var retryCancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                do
                {
                    try
                    {
                        replSetStepDownResult = RunCommand(clientSession, database, command, readPreference, cancellationToken);
                    }
                    catch (MongoCommandException ex) when (IsReplSetStepDownRetryException(ex) && !retryCancellationSource.IsCancellationRequested)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(10));
                    }
                } while (replSetStepDownResult == null);
            }

            replSetStepDownResult.Should().NotBeNull();
            return replSetStepDownResult;
        }

        public async static Task<BsonDocument> RunReplicaSetStepDownAsync(
            IMongoDatabase database,
            BsonDocument command,
            IClientSessionHandle clientSession = null,
            ReadPreference readPreference = null,
            CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(database, nameof(database));
            Ensure.IsNotNull(command, nameof(command));
            Ensure.That(command.TryGetValue("replSetStepDown", out _), "The command must be replSetStepDown.");

            BsonDocument replSetStepDownResult = null;
            using (var retryCancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                do
                {
                    try
                    {
                        replSetStepDownResult = await RunCommandAsync(clientSession, database, command, readPreference, cancellationToken).ConfigureAwait(false);
                    }
                    catch (MongoCommandException ex) when (IsReplSetStepDownRetryException(ex) && !retryCancellationSource.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(10)).ConfigureAwait(false);
                    }
                } while (replSetStepDownResult == null);
            }

            replSetStepDownResult.Should().NotBeNull();
            return replSetStepDownResult;
        }

        // private methods
        private static bool IsReplSetStepDownRetryException(MongoCommandException ex)
        {
            return ex.Message.StartsWith("Command replSetStepDown failed: Unable to acquire lock");
        }

        private static BsonDocument RunCommand(IClientSessionHandle clientSession, IMongoDatabase database, BsonDocument command, ReadPreference readPreference, CancellationToken cancellationToken)
        {
            if (clientSession != null)
            {
                return database.RunCommand<BsonDocument>(clientSession, command, readPreference, cancellationToken);
            }
            else
            {
                return database.RunCommand<BsonDocument>(command, readPreference, cancellationToken);
            }
        }

        private static async Task<BsonDocument> RunCommandAsync(IClientSessionHandle clientSession, IMongoDatabase database, BsonDocument command, ReadPreference readPreference, CancellationToken cancellationToken)
        {
            if (clientSession != null)
            {
                return await database.RunCommandAsync<BsonDocument>(clientSession, command, readPreference, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await database.RunCommandAsync<BsonDocument>(command, readPreference, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
