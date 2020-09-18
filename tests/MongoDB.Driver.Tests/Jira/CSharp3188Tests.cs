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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp3188Tests
    {
        [SkippableTheory]
        [ParameterAttributeData]
        public void Connection_timeout_should_throw_expected_exception([Values(false, true)] bool async)
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo(new SemanticVersion(4, 4, 0)); // failCommand.blockTimeMS is supported since 4.4

            var socketTimeout = TimeSpan.FromMilliseconds(100);
            var serverBlockTime = socketTimeout + TimeSpan.FromMilliseconds(60000);

            var clientSettings = DriverTestConfiguration.GetClientSettings().Clone();
            clientSettings.SocketTimeout = socketTimeout;
            using (var client = DriverTestConfiguration.CreateDisposableClient(clientSettings))
            {
                using (ConfigureFailPoint(client.Cluster, serverBlockTime))
                {
                    var database = client.GetDatabase("database");
                    if (async)
                    {
                        var exception = Record.Exception(() => database.RunCommandAsync<BsonDocument>("{ ping : 1 }").GetAwaiter().GetResult());

                        var mongoConnectionException = exception.Should().BeOfType<MongoConnectionException>().Subject;
                        mongoConnectionException.ContainsSocketTimeoutException.Should().BeFalse();
                        mongoConnectionException.ContainsTimeoutException.Should().BeTrue();
                        mongoConnectionException
                            .InnerException.Should().BeOfType<TimeoutException>().Subject
                            .InnerException.Should().BeNull();
                    }
                    else
                    {
                        var exception = Record.Exception(() => database.RunCommand<BsonDocument>("{ ping : 1 }"));

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

            FailPoint ConfigureFailPoint(ICluster cluster, TimeSpan blockTime)
            {
                var failPointCommand = BsonDocument.Parse($@"
                {{
                    configureFailPoint : 'failCommand',
                    mode : {{
                        times : 1
                    }},
                    data : {{
                        failCommands : ['ping'],
                        blockConnection : true,
                        blockTimeMS : {blockTime.TotalMilliseconds}
                    }}
                }}");

                return FailPoint.Configure(cluster, NoCoreSession.NewHandle(), failPointCommand);
            }
        }
    }
}
