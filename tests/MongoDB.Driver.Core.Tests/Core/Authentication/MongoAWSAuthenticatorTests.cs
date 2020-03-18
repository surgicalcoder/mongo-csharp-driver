/* Copyright 2020–present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using Xunit;
using MongoDB.Driver.Core.Connections;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Authentication;
using Moq;
using MongoDB.Bson.IO;

namespace MongoDB.Driver.Core.Tests.Core.Authentication
{
    public class MongoAWSAuthenticatorTests
    {
        // private constants
        private const int _clientNonceLength = 32;

        // private static
        private static readonly IRandomByteGenerator __randomByteGenerator = new DefaultRandomByteGenerator();

        [Theory]
        [ParameterAttributeData]
        public void AuthenticateShouldWorkAsExpected(
            [Values(false, true)] bool async)
        {
            var dateTime = DateTime.UtcNow;

            var serverId = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017));
            var connectionDescription = new ConnectionDescription(
                new ConnectionId(serverId),
                new IsMasterResult(new BsonDocument("ok", 1).Add("ismaster", 1)),
                new BuildInfoResult(new BsonDocument("version", "4.0.0")));

            var clientNonce = __randomByteGenerator.Generate(_clientNonceLength);
            var serverNonce = Combine(clientNonce, __randomByteGenerator.Generate(_clientNonceLength));
            var host = "sts.amazonaws.com";
            var credential = new UsernamePasswordCredential("$external", "permanentuser", "FAKEFAKEFAKEFAKEFAKEfakefakefakefakefake");
            
            AwsSignatureVersion4.SignRequest(
                dateTime,
                credential.Username,
                credential.GetInsecurePassword(),
                null,
                serverNonce,
                host,
                out var authHeader,
                out var timestamp);

            var dateTimeProviderMock = new Mock<IClock>();
            dateTimeProviderMock.Setup(x => x.UtcNow).Returns(dateTime);

            var randomByteGeneratorMock = new Mock<IRandomByteGenerator>();
            randomByteGeneratorMock.Setup(x => x.Generate(It.IsAny<int>())).Returns(clientNonce);

            var expectedClientFirstMessage = new BsonDocument()
                .Add("r", new BsonBinaryData(clientNonce))
                .Add("p", new BsonInt32('n'));
            var expectedClientSecondMessage = new BsonDocument()
                .Add("a", authHeader)
                .Add("d", timestamp);

            var serverFirstMessage = new BsonDocument()
                .Add("s", new BsonBinaryData(serverNonce))
                .Add("h", new BsonString(host));
            var saslStartReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                $"{{conversationId: 1, done: false, payload: BinData(0,\"{ToBase64(ToBytes(serverFirstMessage))}\"), ok: 1}}"));
            var saslContinueReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                "{conversationId: 1, done: true, payload: BinData(0,\"\"), ok: 1}"));

            var subject = new MongoAWSAuthenticator(credential, null, randomByteGeneratorMock.Object, dateTimeProviderMock.Object);

            var connection = new MockConnection(serverId);
            connection.EnqueueReplyMessage(saslStartReply);
            connection.EnqueueReplyMessage(saslContinueReply);

            var expectedRequestId = RequestMessage.CurrentGlobalRequestId + 1;

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => subject.AuthenticateAsync(connection, connectionDescription, CancellationToken.None).GetAwaiter().GetResult());
            }
            else
            {
                exception = Record.Exception(() => subject.Authenticate(connection, connectionDescription, CancellationToken.None));
            }
            exception.Should().BeNull();

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 2, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(2);

            var actualRequestId0 = sentMessages[0]["requestId"].AsInt32;
            var actualRequestId1 = sentMessages[1]["requestId"].AsInt32;
            actualRequestId0.Should().BeInRange(expectedRequestId, expectedRequestId + 10);
            actualRequestId1.Should().BeInRange(actualRequestId0 + 1, actualRequestId0 + 11);

            var expectedFirstMessage = GetExpectedSaslStartMessage(actualRequestId0, expectedClientFirstMessage);
            var expectedSecondMessage = GetExpectedSaslContinueMessage(actualRequestId1, expectedClientSecondMessage);

            sentMessages[0].Should().Be(expectedFirstMessage);
            sentMessages[1].Should().Be(expectedSecondMessage);
        }

        private static string GetExpectedSaslContinueMessage(int requestId, BsonDocument clientMessage)
        {
            return  "{" +
                        "opcode: \"query\", " +
                        $"requestId: {requestId}, " +
                        "database: \"$external\", " +
                        "collection: \"$cmd\", " +
                        "batchSize: -1, " +
                        "slaveOk: true, " +
                        "query: " +
                        "{ " +
                            "\"saslContinue\" : 1, " +
                            "\"conversationId\" : 1, " +
                            $"\"payload\" : new BinData(0, \"{ToBase64(ToBytes(clientMessage))}\") " +
                        "}" +
                    "}";
        }

        private static string GetExpectedSaslStartMessage(int requestId, BsonDocument clientMessage)
        {
            return  "{" +
                        "opcode: \"query\", " +
                        $"requestId: {requestId}, " +
                        "database: \"$external\", " +
                        "collection: \"$cmd\", " +
                        "batchSize: -1, " +
                        "slaveOk: true, " +
                        "query: " +
                        "{ " +
                            "\"saslStart\" : 1, " +
                            "\"mechanism\" : \"MONGODB-AWS\", " +
                            $"\"payload\" : new BinData(0, \"{ToBase64(ToBytes(clientMessage))}\") " +
                        "}" +
                    "}";
        }

        private static byte[] Combine(byte[] first, byte[] second)
        {
            var result = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, result, 0, first.Length);
            Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
            return result;
        }

        private static string ToBase64(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        private static byte[] ToBytes(BsonDocument doc)
        {
            var settings = new BsonBinaryWriterSettings()
            {
#pragma warning disable 618
                GuidRepresentation = GuidRepresentation.Standard
#pragma warning restore 618
            };
            return doc.ToBson(null, settings);
        }
    }
}
