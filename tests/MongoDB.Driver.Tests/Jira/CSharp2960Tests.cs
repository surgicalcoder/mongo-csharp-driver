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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp2960Tests
    {
        private readonly string _collectionName = DriverTestConfiguration.CollectionNamespace.CollectionName;
        private readonly string _databaseName = DriverTestConfiguration.DatabaseNamespace.DatabaseName;

        [Theory]
        [ParameterAttributeData]
        public void Test(
            [Values(false, true)] bool retryWrites, 
            [Values(false, true)] bool acknowledged,
            [Values(false, true)] bool async)
        {
            using (var client = DriverTestConfiguration.CreateDisposableClient(s => s.RetryWrites = retryWrites))
            {
                var database = client.GetDatabase(_databaseName);
                database.DropCollection(_collectionName);
                var writeConcern = acknowledged ? WriteConcern.Acknowledged : WriteConcern.Unacknowledged;
                var collection = database.GetCollection<BsonDocument>(_collectionName).WithWriteConcern(writeConcern);

                var document = new BsonDocument("_id", 1);
                if (async)
                {
                    collection.InsertOneAsync(document).GetAwaiter().GetResult();
                }
                else
                {
                    collection.InsertOne(document);
                }

                SpinWait.SpinUntil(() => collection.CountDocuments("{}") > 0, TimeSpan.FromSeconds(10)).Should().BeTrue();
            }
        }
    }
}
