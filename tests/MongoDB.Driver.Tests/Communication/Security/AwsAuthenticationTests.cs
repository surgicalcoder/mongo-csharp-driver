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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.TestHelpers;
using System;
using Xunit;

namespace MongoDB.Driver.Tests.Communication.Security
{
    [Trait("Category", "Authentication")]
    [Trait("Category", "AwsMechanism")]
    public class AwsAuthenticationTests
    {
        [Fact]
        public void Aws_authentication_should_behave_as_expected()
        {
            using (var client = DriverTestConfiguration.CreateDisposableClient())
            {
                // test that a command that doesn't require auth completes normally
                var adminDatabase = client.GetDatabase("admin");
                var isMasterCommand = new BsonDocument("ismaster", 1);
                var isMasterResult = adminDatabase.RunCommand<BsonDocument>(isMasterCommand);

                // test that a command that does require auth completes normally
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);
                var count = collection.CountDocuments(FilterDefinition<BsonDocument>.Empty);
            }
        }
    }
}
