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
using System.IO;
using System.Net.Sockets;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp3188Tests
    {
        [SkippableTheory]
        [ParameterAttributeData]
        public void Ensure_that_MongoConnectionException_contains_expected_attributes([Values(false, true)] bool async)
        {
            var serverResponseDelay = TimeSpan.FromMilliseconds(500);

            var mongoClientSettings = DriverTestConfiguration.GetClientSettings().Clone();
            mongoClientSettings.SocketTimeout = TimeSpan.FromMilliseconds(100);

            using (var client = DriverTestConfiguration.CreateDisposableClient(mongoClientSettings))
            {
                var db = client.GetDatabase("db");
                var collectionName = "coll_" + async;
                db.DropCollection(collectionName);
                var coll = db.GetCollection<BsonDocument>(collectionName);
                coll.InsertOne(new BsonDocument());
                // each collection document will trigger delay
                var filterWithDelay = $"{{ $where : 'function() {{ sleep({serverResponseDelay.TotalMilliseconds}); return true; }}' }}";

                if (async)
                {
                    var exception = Record.Exception(() => coll.FindAsync(filterWithDelay).GetAwaiter().GetResult());

                    var mongoConnectionException = exception.Should().BeOfType<MongoConnectionException>().Subject;
                    mongoConnectionException.ContainsSocketTimeoutException.Should().BeFalse();
                    mongoConnectionException.ContainsTimeoutException.Should().BeTrue();
                    mongoConnectionException
                        .InnerException.Should().BeOfType<TimeoutException>().Subject
                        .InnerException.Should().BeNull();
                }
                else
                {
                    var exception = Record.Exception(() => coll.FindSync(filterWithDelay));

                    var mongoConnectionException = exception.Should().BeOfType<MongoConnectionException>().Subject;
                    mongoConnectionException.ContainsSocketTimeoutException.Should().BeTrue();
                    mongoConnectionException.ContainsTimeoutException.Should().BeTrue();
                    var socketException = mongoConnectionException
                        .InnerException.Should().BeOfType<IOException>().Subject
                        .InnerException.Should().BeOfType<SocketException>().Subject;
                    socketException.SocketErrorCode.Should().Be(SocketError.TimedOut);
                    socketException.InnerException.Should().BeNull();
                }
            }
        }
    }
}
