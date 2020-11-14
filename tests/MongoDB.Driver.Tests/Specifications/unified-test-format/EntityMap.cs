/* Copyright 2020-present MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.TestHelpers;

namespace MongoDB.Driver.Tests.Specifications.unified_test_format
{
    // TODO: This class needs to be split into subclasses.
    public sealed class EntityMap : IDisposable
    {
        // private variables
        private readonly Dictionary<string, IGridFSBucket> _buckets = new Dictionary<string, IGridFSBucket>();
        private readonly Dictionary<string, IAsyncCursor<BsonDocument>> _changeStreams = new Dictionary<string, IAsyncCursor<BsonDocument>>();
        private readonly Dictionary<string, EventCapturer> _clientEventCapturers = new Dictionary<string, EventCapturer>();
        private readonly Dictionary<string, DisposableMongoClient> _clients = new Dictionary<string, DisposableMongoClient>();
        private readonly Dictionary<string, IMongoCollection<BsonDocument>> _collections = new Dictionary<string, IMongoCollection<BsonDocument>>();
        private readonly Dictionary<string, IMongoDatabase> _databases = new Dictionary<string, IMongoDatabase>();
        private readonly Dictionary<string, BsonValue> _results = new Dictionary<string, BsonValue>();
        private readonly Dictionary<string, IClientSessionHandle> _sessions = new Dictionary<string, IClientSessionHandle>();

        public EntityMap(BsonArray entitiesArray)
        {
            foreach (var entityItem in entitiesArray)
            {
                if (entityItem.AsBsonDocument.ElementCount != 1)
                {
                    throw new FormatException("Entity item should contain single element");
                }

                var entityType = entityItem.AsBsonDocument.GetElement(0).Name;
                var entity = entityItem[0].AsBsonDocument;
                var id = entity["id"].AsString;
                switch (entityType)
                {
                    case "bucket":
                        if (_buckets.ContainsKey(id))
                        {
                            throw new Exception($"Bucket entity with id '{id}' already exists");
                        }
                        var bucket = CreateBucket(entity);
                        _buckets.Add(id, bucket);
                        break;
                    case "client":
                        if (_clients.ContainsKey(id))
                        {
                            throw new Exception($"Client entity with id '{id}' already exists");
                        }
                        var createClientResult = CreateClient(entity);
                        _clients.Add(id, createClientResult.Item1);
                        _clientEventCapturers.Add(id, createClientResult.Item2);
                        break;
                    case "collection":
                        if (_collections.ContainsKey(id))
                        {
                            throw new Exception($"Collection entity with id '{id}' already exists");
                        }
                        var collection = CreateCollection(entity);
                        _collections.Add(id, collection);
                        break;
                    case "database":
                        if (_databases.ContainsKey(id))
                        {
                            throw new Exception($"Database entity with id '{id}' already exists");
                        }
                        var database = CreateDatabase(entity);
                        _databases.Add(id, database);
                        break;
                    case "session":
                        if (_sessions.ContainsKey(id))
                        {
                            throw new Exception($"Session entity with id '{id}' already exists");
                        }
                        var session = CreateSession(entity);
                        _sessions.Add(id, session);
                        break;
                    default:
                        throw new NotSupportedException($"TODO: '{entityType}'");
                }
            }
        }

        // public methods
        public IGridFSBucket GetBucket(string bucketId)
        {
            return _buckets[bucketId];
        }

        public IMongoClient GetClient(string clientId)
        {
            return _clients[clientId];
        }

        public IMongoCollection<BsonDocument> GetCollection(string collectionId)
        {
            return _collections[collectionId];
        }

        public IMongoDatabase GetDatabase(string databaseId)
        {
            return _databases[databaseId];
        }

        public EventCapturer GetEventCapturer(string clientId)
        {
            return _clientEventCapturers[clientId];
        }

        public BsonValue GetResult(string resultId)
        {
            return _results[resultId];
        }

        public IClientSessionHandle GetSession(string sessionId)
        {
            return _sessions[sessionId];
        }

        public void Dispose()
        {
            foreach (var client in _clients.Values)
            {
                client.Dispose();
            }
        }

        // TODO: Refactor into separate builders

        // private methods
        private IGridFSBucket CreateBucket(BsonDocument entity)
        {
            var databaseId = entity["database"].AsString;
            var database = _databases[databaseId];

            var bucket = new GridFSBucket(database);
            // TODO: process bucket entity args

            return bucket;
        }

        private (DisposableMongoClient, EventCapturer) CreateClient(BsonDocument entity)
        {
            // TODO: Refactor the method
            EventCapturer eventCapturer = null;

            var useMultipleShardRouters = false;
            var retryWrites = true;
            var retryReads = true;
            var readConcern = ReadConcern.Default;
            var writeConcern = WriteConcern.Acknowledged;
            var eventTypesToCapture = new List<string>();
            var commandNamesToSkip = new List<string> { "isMaster", "buildInfo" }; // TODO: Check if those commands could be prevented and not just skipped

            foreach (var element in entity)
            {
                switch (element.Name)
                {
                    case "id":
                        // handled on higher level
                        break;
                    case "useMultipleMongoses":
                        useMultipleShardRouters = element.Value.AsBoolean;
                        break;
                    case "observeEvents":
                        eventCapturer = eventCapturer ?? new EventCapturer();
                        eventTypesToCapture.AddRange(element.Value.AsBsonArray.Select(x => x.AsString));
                        break;
                    case "ignoreCommandMonitoringEvents":
                        eventCapturer = eventCapturer ?? new EventCapturer();
                        commandNamesToSkip.AddRange(element.Value.AsBsonArray.Select(x => x.AsString));
                        break;
                    case "uriOptions":
                        foreach (var option in element.Value.AsBsonDocument)
                        {
                            switch (option.Name)
                            {
                                case "retryWrites":
                                    retryWrites = option.Value.AsBoolean;
                                    break;
                                case "retryReads":
                                    retryReads = option.Value.AsBoolean;
                                    break;
                                case "readConcernLevel":
                                    var levelValue = option.Value.AsString;
                                    var level = (ReadConcernLevel)Enum.Parse(typeof(ReadConcernLevel), levelValue, true);
                                    readConcern = new ReadConcern(level);
                                    break;
                                case "w":
                                    writeConcern = new WriteConcern(option.Value.AsInt32);
                                    break;
                                default:
                                    throw new FormatException($"Unrecognized client uriOption name: '{option.Name}'");
                            }
                        }
                        break;
                    default:
                        throw new FormatException($"Unrecognized client entity field: '{element.Name}'");
                }
            }

            if (eventCapturer != null)
            {
                foreach (var eventTypeToCapture in eventTypesToCapture)
                {
                    switch (eventTypeToCapture)
                    {
                        case "commandStartedEvent":
                            eventCapturer = eventCapturer.Capture<CommandStartedEvent>(x => !commandNamesToSkip.Contains(x.CommandName));
                            break;
                        case "commandSucceededEvent":
                            eventCapturer = eventCapturer.Capture<CommandSucceededEvent>(x => !commandNamesToSkip.Contains(x.CommandName));
                            break;
                        case "commandFailedEvent":
                            eventCapturer = eventCapturer.Capture<CommandFailedEvent>(x => !commandNamesToSkip.Contains(x.CommandName));
                            break;
                        default:
                            throw new FormatException($"Invalid event name: {eventTypeToCapture}");
                    }
                }
            }

            var client = DriverTestConfiguration.CreateDisposableClient(
                settings =>
                {
                    settings.RetryReads = retryReads;
                    settings.RetryWrites = retryWrites;
                    settings.ReadConcern = readConcern;
                    settings.WriteConcern = writeConcern;
                    settings.HeartbeatInterval = TimeSpan.FromMilliseconds(5); // the default value for spec tests
                    if (eventCapturer != null)
                    {
                        settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
                    }
                }, useMultipleShardRouters);

            return (client, eventCapturer);
        }

        private IMongoCollection<BsonDocument> CreateCollection(BsonDocument entity)
        {
            var databaseId = entity["database"].AsString;
            var database = _databases[databaseId];
            var collectionName = entity["collectionName"].AsString;
            // TODO: process collection entity args

            return database.GetCollection<BsonDocument>(collectionName);
        }

        private IMongoDatabase CreateDatabase(BsonDocument entity)
        {
            var clientId = entity["client"].AsString;
            var client = _clients[clientId];
            var databaseName = entity["databaseName"].AsString;
            // TODO: process database entity args

            return client.GetDatabase(databaseName);
        }

        private IClientSessionHandle CreateSession(BsonDocument entity)
        {
            var clientId = entity["client"].AsString;
            var client = _clients[clientId];

            var options = new ClientSessionOptions();
            if (entity.TryGetValue("sessionOptions", out var sessionOptions))
            {
                foreach (var option in sessionOptions.AsBsonDocument)
                {
                    switch (option.Name)
                    {
                        case "causalConsistency":
                            options.CausalConsistency = option.Value.ToBoolean();
                            break;
                        case "defaultTransactionOptions":
                            ReadConcern readConcern = null;
                            ReadPreference readPreference = null;
                            WriteConcern writeConcern = null;
                            foreach (var element in option.Value.AsBsonDocument)
                            {
                                switch (element.Name)
                                {
                                    case "readConcern":
                                        readConcern = ReadConcern.FromBsonDocument(element.Value.AsBsonDocument);
                                        break;
                                    case "readPreference":
                                        readPreference = ReadPreference.FromBsonDocument(element.Value.AsBsonDocument);
                                        break;
                                    case "writeConcern":
                                        writeConcern = WriteConcern.FromBsonDocument(element.Value.AsBsonDocument);
                                        break;
                                    default:
                                        throw new ArgumentException($"Invalid field: '{element.Name}'");
                                }
                            }
                            options.DefaultTransactionOptions = new TransactionOptions(readConcern, readPreference, writeConcern);
                            break;
                        default:
                            throw new FormatException($"Unexpected session option: '{option.Name}'");
                    }
                }
            }

            return client.StartSession(options);
        }
    }
}
