/* Copyright 2018-present MongoDB Inc.
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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public abstract class JsonDrivenCollectionTest : JsonDrivenCommandTest
    {
        // protected fields
        protected IMongoCollection<BsonDocument> _collection;

        // constructors
        protected JsonDrivenCollectionTest(IMongoCollection<BsonDocument> collection, Dictionary<string, object> objectMap)
            : base(objectMap)
        {
            _collection = collection;
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureFieldEquals(document, "object", "collection");

            if (document.TryGetValue("databaseOptions", out var databaseOptions))
            {
                ParseDatabaseOptions(databaseOptions.AsBsonDocument, out var database);
                _collection = GetCollectionFromAnotherDatabaseInstance(database, _collection);
            }

            if (document.TryGetValue("collectionOptions", out var collectionOptions))
            {
                ParseCollectionOptions(collectionOptions.AsBsonDocument);
            }

            base.Arrange(document);
        }

        // private methods
        private IMongoCollection<BsonDocument> GetCollectionFromAnotherDatabaseInstance(IMongoDatabase database, IMongoCollection<BsonDocument> collection)
        {
            return database.GetCollection<BsonDocument>(
                collection.CollectionNamespace.CollectionName,
                collection.Settings);
        }

        private void ParseCollectionOptions(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "readConcern", "readPreference", "writeConcern");

            if (document.Contains("readConcern"))
            {
                var readConcern = ReadConcern.FromBsonDocument(document["readConcern"].AsBsonDocument);
                _collection = _collection.WithReadConcern(readConcern);
            }

            if (document.Contains("readPreference"))
            {
                var readPreference = ReadPreference.FromBsonDocument(document["readPreference"].AsBsonDocument);
                _collection = _collection.WithReadPreference(readPreference);
            }

            if (document.Contains("writeConcern"))
            {
                var writeConcern = WriteConcern.FromBsonDocument(document["writeConcern"].AsBsonDocument);
                _collection = _collection.WithWriteConcern(writeConcern);
            }
        }

        private void ParseDatabaseOptions(BsonDocument document, out IMongoDatabase database)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "readConcern", "readPreference", "writeConcern");

            database = _collection.Database;
            if (document.Contains("readConcern"))
            {
                var readConcern = ReadConcern.FromBsonDocument(document["readConcern"].AsBsonDocument);
                database = database.WithReadConcern(readConcern);
            }

            if (document.Contains("readPreference"))
            {
                var readPreference = ReadPreference.FromBsonDocument(document["readPreference"].AsBsonDocument);
                database = database.WithReadPreference(readPreference);
            }

            if (document.Contains("writeConcern"))
            {
                var writeConcern = WriteConcern.FromBsonDocument(document["writeConcern"].AsBsonDocument);
                database = database.WithWriteConcern(writeConcern);
            }
        }
    }
}
