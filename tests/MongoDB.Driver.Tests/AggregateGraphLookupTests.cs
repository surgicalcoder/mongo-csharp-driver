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

using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class AggregateGraphLookupTests
    {
        // private fields
        private readonly IMongoDatabase _database;

        // constructors
        public AggregateGraphLookupTests()
        {
            var client = DriverTestConfiguration.Client;
            var databaseName = CoreTestConfiguration.DatabaseNamespace.DatabaseName;
            _database = client.GetDatabase(databaseName);
        }

        // public methods
        [SkippableFact]
        public void GraphLookup_with_many_to_one_parameters_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateGraphLookupStage);
            var collectionName = "collectionC";
            EnsureTestDataC(_database, collectionName);
            var expectedResult = new CMap[]
            {
                new CMap
                {
                    From = new X[] { new X(2), new X(3) },
                    To = new X(1),
                    Map = new List<C> { new C { From = new X[] { new X(3), new X(4) }, To = new X(2) } }
                },
                new CMap
                {
                    From = new X[] { new X(3), new X(4) },
                    To = new X(2),
                    Map = new List<C>()
                }
            };
            var collection = _database.GetCollection<C>(collectionName);

            var result = collection
                .Aggregate()
                .GraphLookup(
                    from: collection,
                    connectFromField: x => x.From,
                    connectToField: x => x.To,
                    startWith: x => x.From,
                    @as: (CMap x) => x.Map)
                .ToList();

            result.Count.Should().Be(2);
            result[0].ToBsonDocument().Should().Be(expectedResult[0].ToBsonDocument());
            result[1].ToBsonDocument().Should().Be(expectedResult[1].ToBsonDocument());
        }

        [SkippableFact]
        public void GraphLookup_with_one_to_many_parameters_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateGraphLookupStage);
            var collectionName = "collectionB";
            EnsureTestDataB(_database, collectionName);
            var expectedResult = new BMap[]
            {
                new BMap
                {
                    From = new X(1),
                    To = new X[] { new X(2), new X(3) },
                    Map = new List<B>()
                },
                new BMap
                {
                    From = new X(2),
                    To = new X[] { new X(3), new X(4) },
                    Map = new List<B> { new B { From = new X(1), To = new X[] { new X(2), new X(3) } } }
                }
            };
            var collection = _database.GetCollection<B>(collectionName);

            var result = collection
                .Aggregate()
                .GraphLookup(
                    from: collection,
                    connectFromField: x => x.From,
                    connectToField: x => x.To,
                    startWith: x => x.From,
                    @as: (BMap x) => x.Map)
                .ToList();

            result.Count.Should().Be(2);
            result[0].ToBsonDocument().Should().Be(expectedResult[0].ToBsonDocument());
            result[1].ToBsonDocument().Should().Be(expectedResult[1].ToBsonDocument());
        }

        [SkippableFact]
        public void GraphLookup_with_one_to_one_parameters_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateGraphLookupStage);
            var collectionName = "collectionA";
            EnsureTestDataA(_database, collectionName);
            var expectedResult = new AMap[]
            {
                new AMap
                {
                    From = new X(1),
                    Map = new List<A>(),
                    To = new X(2)
                },
                new AMap
                {
                    From = new X(2),
                    To = new X(3),
                    Map = new List<A> { new A { From = new X(1), To = new X(2) } }
                }
            };
            var collection = _database.GetCollection<A>(collectionName);

            var result = collection
                .Aggregate()
                .GraphLookup(
                    from: collection,
                    connectFromField: x => x.From,
                    connectToField: x => x.To,
                    startWith: x => x.From,
                    @as: (AMap x) => x.Map)
                .ToList();

            result.Count.Should().Be(2);
            result[0].ToBsonDocument().Should().Be(expectedResult[0].ToBsonDocument());
            result[1].ToBsonDocument().Should().Be(expectedResult[1].ToBsonDocument());
        }

        // private methods
        private void EnsureTestDataA(IMongoDatabase database, string collectionName)
        {
            database.DropCollection(collectionName);
            var collection = database.GetCollection<A>(collectionName);
            var documents = new A[]
            {
                new A { From = new X(1), To = new X(2) },
                new A { From = new X(2), To = new X(3) },
            };
            collection.InsertMany(documents);
        }

        private void EnsureTestDataB(IMongoDatabase database, string collectionName)
        {
            database.DropCollection(collectionName);
            var collection = database.GetCollection<B>(collectionName);
            var documents = new B[]
            {
                new B { From = new X(1), To = new X[] { new X(2), new X(3) } },
                new B { From = new X(2), To = new X[] { new X(3), new X(4) } },
            };
            collection.InsertMany(documents);
        }

        private void EnsureTestDataC(IMongoDatabase database, string collectionName)
        {
            database.DropCollection(collectionName);
            var collection = database.GetCollection<C>(collectionName);
            var documents = new C[]
            {
                new C { From = new X[] { new X(2), new X(3) }, To = new X(1) },
                new C { From = new X[] { new X(3), new X(4) }, To = new X(2) },
            };
            collection.InsertMany(documents);
        }

        // nested types
        [BsonIgnoreExtraElements]
        private class A
        {
            public X From { get; set; }
            public X To { get; set; }
        }

        [BsonIgnoreExtraElements]
        private class AMap
        {
            public X From { get; set; }
            public X To { get; set; }
            public List<A> Map { get; set; }
        }

        [BsonIgnoreExtraElements]
        private class B
        {
            public X From { get; set; }
            public IEnumerable<X> To { get; set; }
        }

        [BsonIgnoreExtraElements]
        private class BMap
        {
            public X From { get; set; }
            public IEnumerable<X> To { get; set; }
            public List<B> Map { get; set; }
        }

        [BsonIgnoreExtraElements]
        private class C
        {
            public IEnumerable<X> From { get; set; }
            public X To { get; set; }
        }

        [BsonIgnoreExtraElements]
        private class CMap
        {
            public IEnumerable<X> From { get; set; }
            public X To { get; set; }
            public List<C> Map { get; set; }
        }

        private class X
        {
            public int Value { get; set; }

            public X(int value)
            {
                Value = value;
            }
        }
    }
}
