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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Servers
{
    public class RoundTripTimeMonitorTests
    {
        private static EndPoint __endPoint = new DnsEndPoint("localhost", 27017);
        private static ServerId __serverId = new ServerId(new ClusterId(), __endPoint);

        [Fact]
        public void Average_should_grow()
        {
            var frequency = TimeSpan.FromMilliseconds(10);
            var mockConnection = new Mock<IConnection>();

            var subject = CreateSubject(
                frequency,
                mockConnection,
                CancellationToken.None,
                out var mockConnectionFactory);

            TimeSpan previousAverage = TimeSpan.Zero;
            mockConnection
                .SetupSequence(c => c.ReceiveMessageAsync(It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), It.IsAny<MessageEncoderSettings>(), It.IsAny<CancellationToken>()))
                .Returns(
                    () =>
                    {
                        return Task.FromResult(CreateResponseMessage());
                    })
                .Returns(
                    () =>
                    {
                        subject.Average.Should().BeGreaterThan(previousAverage);
                        previousAverage = subject.Average;
                        return Task.FromResult(CreateResponseMessage());
                    })
                .Returns(
                    () =>
                    {
                        subject.Average.Should().BeGreaterThan(previousAverage);
                        previousAverage = subject.Average;
                        return Task.FromResult(CreateResponseMessage());
                    })
                .Returns(
                    () =>
                    {
                        subject.Average.Should().BeGreaterThan(previousAverage);
                        previousAverage = subject.Average;
                        subject._disposed(true); // stop the loop
                        return Task.FromResult(CreateResponseMessage());
                    });

            subject.RunAsync().ConfigureAwait(false);
        }

        [Fact]
        public void Constructor_should_throw_connection_endpoint_is_null()
        {
            var exception = Record.Exception(() => new RoundTripTimeMonitor(Mock.Of<IConnectionFactory>(), __serverId, null, TimeSpan.Zero, CancellationToken.None));
            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("endpoint");
        }

        [Fact]
        public void Constructor_should_throw_connection_factory_is_null()
        {
            var exception = Record.Exception(() => new RoundTripTimeMonitor(null, __serverId, __endPoint, TimeSpan.Zero, CancellationToken.None));
            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("connectionFactory");
        }

        [Fact]
        public void Constructor_should_throw_connection_serverId_is_null()
        {
            var exception = Record.Exception(() => new RoundTripTimeMonitor(Mock.Of<IConnectionFactory>(), null, __endPoint, TimeSpan.Zero, CancellationToken.None));
            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("serverId");
        }

        [Fact]
        public void Dispose_should_close_connection_only_once()
        {
            var subject = CreateSubject(
                TimeSpan.FromMilliseconds(10),
                CancellationToken.None,
                out var mockConnectionFactory,
                out var mockConnection);

            subject.RunAsync().ConfigureAwait(false);
            SpinWait.SpinUntil(() => subject._roundTripTimeConnection() != null, TimeSpan.FromSeconds(2)).Should().BeTrue();

            subject.Dispose();
            subject._roundTripTimeConnection().Should().BeNull();

            mockConnection.Verify(c => c.Dispose(), Times.Once);
            subject._disposed().Should().BeTrue();
        }

        [Fact]
        public void Failed_isMaster_should_close_connection_only()
        {
            var frequency = TimeSpan.FromMilliseconds(10);
            var mockConnection = new Mock<IConnection>();

            var subject = CreateSubject(
                frequency,
                mockConnection,
                CancellationToken.None,
                out var mockConnectionFactory);

            mockConnection
                .SetupSequence(c => c.ReceiveMessageAsync(It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), It.IsAny<MessageEncoderSettings>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("TestMessage"))  // step 1
                .Returns(                              // step 2
                    () =>
                    {
                        return Task.FromResult(CreateResponseMessage());
                    })
                .Returns(                              // step 3
                    () =>
                    {
                        subject._disposed(true); // stop the loop
                        return Task.FromResult(CreateResponseMessage());
                    });

            subject.RunAsync().ConfigureAwait(false);

            SpinWait.SpinUntil(
                () => subject._roundTripTimeConnection() != null,
                TimeSpan.FromSeconds(2))
                .Should()
                .BeTrue(); // waiting for initial connection initialization

            SpinWait.SpinUntil(
                () => subject._roundTripTimeConnection() == null,
                TimeSpan.FromSeconds(2))
                .Should()
                .BeTrue(); // Step 1. Waiting for connection disposing after exception
            mockConnection.Verify(c => c.Dispose(), Times.Once);
            subject._disposed().Should().BeFalse();

            SpinWait.SpinUntil(
                () => subject._roundTripTimeConnection() != null,
                TimeSpan.FromSeconds(2))
                .Should()
                .BeTrue(); // Step 2. Restored

            // step 3. Just close the loop.
        }

        // private methods
        private RoundTripTimeMonitor CreateSubject(
            TimeSpan frequency,
            Mock<IConnection> mockConnection,
            CancellationToken cancellationToken,
            out Mock<IConnectionFactory> mockConnectionFactory)
        {
            var isMasterDocument = new BsonDocument
            {
                { "ok", 1 },
            };

            ConnectionId connectId;
            mockConnection
                .SetupGet(c => c.Description)
                .Returns(new ConnectionDescription(
                    connectId = new ConnectionId(__serverId, 0),
                    new IsMasterResult(isMasterDocument),
                    new BuildInfoResult(BsonDocument.Parse("{ ok : 1, version : '4.4.0' }"))));

            mockConnection
               .SetupGet(c => c.ConnectionId)
               .Returns(connectId);

            mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory
                .Setup(f => f.CreateConnection(__serverId, __endPoint))
                .Returns(mockConnection.Object);

            return new RoundTripTimeMonitor(
                mockConnectionFactory.Object,
                __serverId,
                __endPoint,
                frequency,
                cancellationToken);
        }

        private RoundTripTimeMonitor CreateSubject(TimeSpan frequency, CancellationToken cancellationToken, out Mock<IConnectionFactory> mockConnectionFactory, out Mock<IConnection> mockConnection)
        {
            mockConnection = new Mock<IConnection>();
            mockConnection
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), It.IsAny<MessageEncoderSettings>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => CreateResponseMessage());

            return CreateSubject(frequency, mockConnection, cancellationToken, out mockConnectionFactory);
        }

        private ResponseMessage CreateResponseMessage()
        {
            var section0Document = "{ ismaster : true, topologyVersion : { processId : ObjectId('5ee3f0963109d4fe5e71dd28'), counter : NumberLong(0) }, ok : 1.0 }";
            var section0 = new Type0CommandMessageSection<RawBsonDocument>(
                new RawBsonDocument(BsonDocument.Parse(section0Document).ToBson()),
                RawBsonDocumentSerializer.Instance);
            return new CommandResponseMessage(new CommandMessage(1, 1, new[] { section0 }, false));
        }
    }

    internal static class RoundTripTimeMonitorReflector
    {
        public static bool _disposed(this RoundTripTimeMonitor roundTripTimeMonitor)
        {
            return (bool)Reflector.GetFieldValue(roundTripTimeMonitor, nameof(_disposed));
        }

        public static void _disposed(this RoundTripTimeMonitor roundTripTimeMonitor, bool value)
        {
            Reflector.SetFieldValue(roundTripTimeMonitor, nameof(_disposed), value);
        }

        public static IConnection _roundTripTimeConnection(this RoundTripTimeMonitor roundTripTimeMonitor)
        {
            return (IConnection)Reflector.GetFieldValue(roundTripTimeMonitor, nameof(_roundTripTimeConnection));
        }
    }
}
