/* Copyright 2016-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing,.Setup software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Servers
{
    public class ServerMonitorTests
    {
        private EndPoint _endPoint;
        private CancellationTokenSource _cancellationTokenSource;
        private MockConnection _connection;
        private Mock<IConnectionFactory> _mockConnectionFactory;
        private Mock<IRoundTripTimeMonitor> _mockRoundTripTimeMonitor;
        private EventCapturer _capturedEvents;
        private ServerId _serverId;
        private ServerMonitor _subject;

        public ServerMonitorTests()
        {
            _endPoint = new DnsEndPoint("localhost", 27017);
            _serverId = new ServerId(new ClusterId(), _endPoint);
            _capturedEvents = new EventCapturer();
            _cancellationTokenSource = new CancellationTokenSource();

            _subject = CreateSubject(eventCapturer: _capturedEvents);
        }

        [Fact]
        public void Constructor_should_throw_when_serverId_is_null()
        {
            Action act = () => new ServerMonitor(null, _endPoint, _mockConnectionFactory.Object, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_endPoint_is_null()
        {
            Action act = () => new ServerMonitor(_serverId, null, _mockConnectionFactory.Object, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_connectionFactory_is_null()
        {
            Action act = () => new ServerMonitor(_serverId, _endPoint, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_eventSubscriber_is_null()
        {
            Action act = () => new ServerMonitor(_serverId, _endPoint, _mockConnectionFactory.Object, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_roundTripTimeMonitor_is_null()
        {
            var exception = Record.Exception(() => new ServerMonitor(_serverId, _endPoint, _mockConnectionFactory.Object, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, _capturedEvents, roundTripTimeMonitor: null, Mock.Of<CancellationTokenSource>()));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Description_should_return_default_when_uninitialized()
        {
            var description = _subject.Description;

            description.EndPoint.Should().Be(_endPoint);
            description.Type.Should().Be(ServerType.Unknown);
            description.State.Should().Be(ServerState.Disconnected);
        }

        [Fact]
        public void Description_should_return_default_when_disposed()
        {
            _subject.Dispose();

            var description = _subject.Description;

            description.EndPoint.Should().Be(_endPoint);
            description.Type.Should().Be(ServerType.Unknown);
            description.State.Should().Be(ServerState.Disconnected);
        }

        [Fact]
        public void DescriptionChanged_should_be_raised_during_initial_handshake()
        {
            var changes = new List<ServerDescriptionChangedEventArgs>();
            _subject.DescriptionChanged += (o, e) => changes.Add(e);

            SetupHeartbeatConnection();
            _subject.Initialize();
            SpinWait.SpinUntil(
                () =>
                    _subject.Description.State == ServerState.Connected &&
                    changes.Count > 0, // there is a small possible delay between triggering an event and the actual description changing
                    TimeSpan.FromSeconds(5))
                .Should()
                .BeTrue();

            changes.Count.Should().Be(1);
            changes[0].OldServerDescription.State.Should().Be(ServerState.Disconnected);
            changes[0].NewServerDescription.State.Should().Be(ServerState.Connected);

            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Description_should_be_connected_after_successful_heartbeat()
        {
            SetupHeartbeatConnection();
            _subject.Initialize();
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Connected, TimeSpan.FromSeconds(5)).Should().BeTrue();

            _subject.Description.State.Should().Be(ServerState.Connected);
            _subject.Description.Type.Should().Be(ServerType.Standalone);

            // no ServerHeartbeat events should be triggered during initial handshake
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Dispose_should_clear_all_resources_only_once()
        {
            _connection = new MockConnection(_serverId, new ConnectionSettings(), _capturedEvents);
            _subject = CreateSubject(_connection);

            SetupHeartbeatConnection();
            _subject.Initialize();
            SpinWait.SpinUntil(() => _subject._connection() != null, TimeSpan.FromSeconds(5)).Should().BeTrue();

            _subject.Dispose();

            for (int attempt = 1; attempt <= 2; attempt++)
            {
                _capturedEvents.Events.Count(e => e is ConnectionClosingEvent).Should().Be(1);
                _cancellationTokenSource.IsCancellationRequested.Should().BeTrue();
                _mockRoundTripTimeMonitor.Verify(m => m.Dispose(), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeIsMasterProtocol_should_use_streaming_protocol_when_available([Values(false, true)] bool isStreamable)
        {
            SetupHeartbeatConnection(isStreamable, autoFillStreamingResponses: true);

            _connection.WasReadTimeoutChanged.Should().Be(null);
            var resultProtocol = _subject.InitializeIsMasterProtocol(_connection);
            if (isStreamable)
            {
                _connection.WasReadTimeoutChanged.Should().BeTrue();
                resultProtocol._command().Should().Contain("isMaster");
                resultProtocol._command().Should().Contain("topologyVersion");
                resultProtocol._command().Should().Contain("maxAwaitTimeMS");
                resultProtocol._responseHandling().Should().Be(CommandResponseHandling.ExhaustAllowed);
            }
            else
            {
                _connection.WasReadTimeoutChanged.Should().Be(null);
                resultProtocol._command().Should().Contain("isMaster");
                resultProtocol._command().Should().NotContain("topologyVersion");
                resultProtocol._command().Should().NotContain("maxAwaitTimeMS");
                resultProtocol._responseHandling().Should().Be(CommandResponseHandling.Return);
            }
        }

        [Fact]
        public void Initialize_should_run_round_time_trip_monitor_only_once()
        {
            SetupHeartbeatConnection();

            _subject.Initialize();
            _subject.Initialize();

            _mockRoundTripTimeMonitor.Verify(m => m.RunAsync(), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("MongoConnectionException")]
        public void Heartbeat_should_make_immediate_next_attempt_for_streaming_protocol(string exceptionType)
        {
            _subject = CreateSubject(
                eventCapturer: new EventCapturer()
                    .Capture<ServerHeartbeatSucceededEvent>()
                    .Capture<ServerHeartbeatFailedEvent>()
                    .Capture<ServerDescriptionChangedEvent>());
            _subject.DescriptionChanged +=
                (o, e) =>
                {
                    _capturedEvents.TryGetEventHandler<ServerDescriptionChangedEvent>(out var handler);
                    handler(new ServerDescriptionChangedEvent(e.OldServerDescription, e.NewServerDescription));
                };

            SetupHeartbeatConnection(isStreamable: true, autoFillStreamingResponses: false);

            Exception exception = null;
            switch (exceptionType)
            {
                case null:
                    _connection.EnqueueCommandResponseMessage(CreateStreamableCommandResponseMessage(), null);
                    break;
                case "MongoConnectionException":
                    // previousDescription type is "Known" for this case
                    _connection.EnqueueCommandResponseMessage(
                        exception = CoreExceptionHelper.CreateException(exceptionType));
                    break;
            }

            // 10 seconds delay. Not expected to be processe
            _connection.EnqueueCommandResponseMessage(CreateStreamableCommandResponseMessage(), TimeSpan.FromSeconds(10));

            _subject.Initialize();

            _capturedEvents.WaitForOrThrowIfTimeout(
                events =>
                    events.Count(e => e is ServerDescriptionChangedEvent) > 1,  // the connection has been initialized and the first heatbeat event has been fired
                TimeSpan.FromSeconds(5));

            _capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>(); // connection initialized
            AssertHeartbeatAttempt();
            _capturedEvents.Any().Should().BeFalse(); // the next attempt will be in 10 seconds because the second stremable respone has 10 seconds delay

            void AssertHeartbeatAttempt()
            {
                if (exception != null)
                {
                    _mockRoundTripTimeMonitor.Verify(c => c.Reset(), Times.Once);

                    var serverHeartbeatFailedEvent = _capturedEvents.Next().Should().BeOfType<ServerHeartbeatFailedEvent>().Subject; // updating the server based on the heartbeat
                    serverHeartbeatFailedEvent.Exception.Should().Be(exception);

                    var serverDescriptionChangedEvent = _capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>().Subject;
                    serverDescriptionChangedEvent.NewDescription.HeartbeatException.Should().Be(exception);

                    serverDescriptionChangedEvent = _capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>().Subject;  // when we catch exceptions, we close the current connection, so opening connection will trigger one more ServerDescriptionChangedEvent
                    serverDescriptionChangedEvent.OldDescription.HeartbeatException.Should().Be(exception);
                    serverDescriptionChangedEvent.NewDescription.HeartbeatException.Should().BeNull();
                }
                else
                {
                    _mockRoundTripTimeMonitor.Verify(c => c.Reset(), Times.Never);
                    _capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>();
                    var serverDescriptionChangedEvent = _capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>().Subject; // updating the server based on the heartbeat
                    serverDescriptionChangedEvent.NewDescription.HeartbeatException.Should().BeNull();
                }
            }
        }

        [Fact]
        public void RequestHeartbeat_should_force_another_heartbeat()
        {
            SetupHeartbeatConnection();
            _subject.Initialize();
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Connected, TimeSpan.FromSeconds(5)).Should().BeTrue();
            _capturedEvents.Clear();

            _subject.RequestHeartbeat();

            // the next requests down heartbeat connection will fail, so the state should
            // go back to disconnected
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Disconnected, TimeSpan.FromSeconds(5)).Should().BeTrue();

            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatFailedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        // private methods
        private ServerMonitor CreateSubject(MockConnection connection = null, EventCapturer eventCapturer = null)
        {
            _mockRoundTripTimeMonitor = new Mock<IRoundTripTimeMonitor>();
            _mockRoundTripTimeMonitor.Setup(m => m.RunAsync()).Returns(Task.FromResult(true));

            _connection = connection ?? new MockConnection();
            _mockConnectionFactory = new Mock<IConnectionFactory>();
            _mockConnectionFactory
                .Setup(f => f.CreateConnection(_serverId, _endPoint))
                .Returns(_connection);

            return new ServerMonitor(
                _serverId,
                _endPoint,
                _mockConnectionFactory.Object,
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan,
                _capturedEvents = (eventCapturer ?? _capturedEvents),
                _mockRoundTripTimeMonitor.Object,
                _cancellationTokenSource);
        }

        private void SetupHeartbeatConnection(bool isStreamable = false, bool autoFillStreamingResponses = true)
        {
            var isMasterDocument = new BsonDocument
            {
                { "ok", 1 },
                { "topologyVersion", new TopologyVersion(new ObjectId(), 0).ToBsonDocument(), isStreamable },
                { "maxAwaitTimeMS", 5000, isStreamable }
            };

            var streamingIsMaster = Feature.StreamingIsMaster;
            var version = isStreamable ? streamingIsMaster.FirstSupportedVersion : streamingIsMaster.LastNotSupportedVersion;
            _connection.Description = new ConnectionDescription(
                _connection.ConnectionId,
                new IsMasterResult(isMasterDocument),
                new BuildInfoResult(BsonDocument.Parse($"{{ ok : 1, version : '{version}' }}")));

            if (autoFillStreamingResponses && isStreamable)
            {
                // immediate attempt
                _connection.EnqueueCommandResponseMessage(CreateStreamableCommandResponseMessage(), null);

                // 10 seconds delay. Won't expected to be processed
                _connection.EnqueueCommandResponseMessage(CreateStreamableCommandResponseMessage(), TimeSpan.FromSeconds(10));
            }
        }

        private CommandResponseMessage CreateStreamableCommandResponseMessage()
        {
            var section0 = "{ ismaster : true, topologyVersion : { processId : ObjectId('5ee3f0963109d4fe5e71dd28'), counter : NumberLong(0) }, ok : 1.0 }";
            var bsonDocument = BsonDocument.Parse(section0);
            return MessageHelper.BuildCommandResponse(new RawBsonDocument(bsonDocument.ToBson()));
        }
    }

    internal static class ServerMonitorReflector
    {
        public static IConnection _connection(this ServerMonitor serverMonitor)
        {
            return (IConnection)Reflector.GetFieldValue(serverMonitor, nameof(_connection));
        }

        public static CommandWireProtocol<BsonDocument> InitializeIsMasterProtocol(this ServerMonitor serverMonitor, IConnection connection)
        {
            return (CommandWireProtocol<BsonDocument>)Reflector.Invoke(serverMonitor, nameof(InitializeIsMasterProtocol), connection);
        }
    }

    internal static class CommandWireProtocolReflector
    {
        public static BsonDocument _command(this CommandWireProtocol<BsonDocument> commandWireProtocol)
        {
            return (BsonDocument)Reflector.GetFieldValue(commandWireProtocol, nameof(_command));
        }

        public static CommandResponseHandling _responseHandling(this CommandWireProtocol<BsonDocument> commandWireProtocol)
        {
            return (CommandResponseHandling)Reflector.GetFieldValue(commandWireProtocol, nameof(_responseHandling));
        }
    }
}
