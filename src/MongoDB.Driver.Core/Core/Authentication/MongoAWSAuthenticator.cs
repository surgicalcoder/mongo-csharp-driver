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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// The Mongo AWS authenticator.
    /// </summary>
    public class MongoAWSAuthenticator : SaslAuthenticator
    {
        // constants
        private const int ClientNonceLength = 32;

        // static properties
        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        /// <value>
        /// The name of the mechanism.
        /// </value>
        public static string MechanismName
        {
            get { return "MONGODB-AWS"; }
        }

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GssapiAuthenticator"/> class.
        /// </summary>
        /// <param name="credential">The credentials.</param>
        /// <param name="properties">The properties.</param>
        public MongoAWSAuthenticator(UsernamePasswordCredential credential, IEnumerable<KeyValuePair<string, string>> properties)
            : this(credential, properties, new DefaultRandomByteGenerator(), SystemClock.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GssapiAuthenticator"/> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="properties">The properties.</param>
        public MongoAWSAuthenticator(string username, IEnumerable<KeyValuePair<string, string>> properties)
            : this(username, properties, new DefaultRandomByteGenerator(), SystemClock.Instance)
        {
        }

        internal MongoAWSAuthenticator(
            UsernamePasswordCredential credential,
            IEnumerable<KeyValuePair<string, string>> properties,
            IRandomByteGenerator randomByteGenerator,
            IClock dateTimeProvider)
            : base(CreateMechanism(credential, properties, randomByteGenerator, dateTimeProvider))
        {
        }

        internal MongoAWSAuthenticator(
            string username,
            IEnumerable<KeyValuePair<string, string>> properties,
            IRandomByteGenerator randomByteGenerator,
            IClock dateTimeProvider)
            : base(CreateMechanism(username, null, properties, randomByteGenerator, dateTimeProvider))
        {
        }

        /// <inheritdoc/>
        public override string DatabaseName
        {
            get { return "$external"; }
        }

        // private static methods
        private static MongoAWSMechanism CreateMechanism(
            UsernamePasswordCredential credential,
            IEnumerable<KeyValuePair<string, string>> properties,
            IRandomByteGenerator randomByteGenerator,
            IClock dateTimeProvider)
        {
            if (credential.Source != "$external")
            {
                throw new ArgumentException("MONGO AWS authentication may only use the $external source.", nameof(credential));
            }

            return CreateMechanism(credential.Username, credential.Password, properties, randomByteGenerator, dateTimeProvider);
        }

        private static MongoAWSMechanism CreateMechanism(
            string username,
            SecureString securePassword,
            IEnumerable<KeyValuePair<string, string>> properties,
            IRandomByteGenerator randomByteGenerator,
            IClock dateTimeProvider)
        {
            var relativeUri = Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_RELATIVE_URI");
            var awsCredentialsCreators = new Func<AwsCredentials>[]
            {
                () => CreateAwsCredentialsFromMongoCredentials(username, securePassword, properties),
                () => CreateAwsCredentialsFromEnvironmentVariables(),
                () => CreateAwsCredentialsFromEcsResponse(relativeUri),
                () => CreateAwsCredentialsFromEc2Response()
            };

            foreach (var awsCredentialsCreator in awsCredentialsCreators)
            {
                var awsCredentials = awsCredentialsCreator();

                if (awsCredentials == null)
                {
                    continue;
                }

                ValidateAwsCredentials(awsCredentials);

                if (awsCredentials.Username == null)
                {
                    continue;
                }

                var credentials = new UsernamePasswordCredential("$external", awsCredentials.Username, awsCredentials.Password);

                return new MongoAWSMechanism(credentials, awsCredentials.SessionToken, randomByteGenerator, dateTimeProvider);
            }

            throw new ArgumentException("A MONGODB-AWS must have access key ID.");
        }

        private static AwsCredentials CreateAwsCredentialsFromMongoCredentials(string username, SecureString securePassword, IEnumerable<KeyValuePair<string, string>> properties)
        {
            var sessionToken = ExtractSessionTokenFromMechanismProperties(properties);

            return new AwsCredentials
            {
                Username = username,
                Password = securePassword,
                SessionToken = sessionToken
            };
        }

        private static AwsCredentials CreateAwsCredentialsFromEnvironmentVariables()
        {
            var username = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var password = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            var sessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");

            return new AwsCredentials
            {
                Username = username,
                Password = ToSecureString(password),
                SessionToken = sessionToken
            };
        }

        private static AwsCredentials CreateAwsCredentialsFromEcsResponse(string relativeUri)
        {
            if (relativeUri == null)
            {
                return null;
            }

            var response = AwsHttpClientHelper.GetECSResponse(relativeUri).GetAwaiter().GetResult();
            var parsedReponse = BsonDocument.Parse(response);
            var username = parsedReponse.GetValue("AccessKeyId")?.AsString;
            var password = parsedReponse.GetValue("SecretAccessKey")?.AsString;
            var sessionToken = parsedReponse.GetValue("Token")?.AsString;

            return new AwsCredentials
            {
                Username = username,
                Password = ToSecureString(password),
                SessionToken = sessionToken
            };
        }

        private static AwsCredentials CreateAwsCredentialsFromEc2Response()
        {
            var response = AwsHttpClientHelper.GetEC2Response().GetAwaiter().GetResult();
            var parsedReponse = BsonDocument.Parse(response);
            var username = parsedReponse.GetValue("AccessKeyId")?.AsString;
            var password = parsedReponse.GetValue("SecretAccessKey")?.AsString;
            var sessionToken = parsedReponse.GetValue("Token")?.AsString;

            return new AwsCredentials
            {
                Username = username,
                Password = ToSecureString(password),
                SessionToken = sessionToken
            };
        }

        private static string ExtractSessionTokenFromMechanismProperties(IEnumerable<KeyValuePair<string, string>> properties)
        {
            string sessionToken = null;
            if (properties != null)
            {
                foreach (var pair in properties)
                {
                    switch (pair.Key.ToUpperInvariant())
                    {
                        case "AWS_SESSION_TOKEN":
                            sessionToken = (string)pair.Value;
                            break;
                        default:
                            throw new ArgumentException($"Unknown AWS property '{pair.Key}'.", nameof(properties));
                    }
                }
            }

            return sessionToken;
        }

        private static SecureString ToSecureString(string str)
        {
            if (str == null)
            {
                return null;
            }

            var secureString = new SecureString();
            foreach (var c in str)
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly();

            return secureString;
        }

        private static void ValidateAwsCredentials(AwsCredentials awsCredentials)
        {
            if (awsCredentials.Username == null && (awsCredentials.Password != null || awsCredentials.SessionToken != null))
            {
                throw new ArgumentException("A MONGODB-AWS must have access key id.");
            }
            if (awsCredentials.Username != null && awsCredentials.Password == null)
            {
                throw new ArgumentException("A MONGODB-AWS must have secret access key.");
            }
        }

        private static BsonDocument ToDocument(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            using (var jsonReader = new BsonBinaryReader(stream))
            {
                var context = BsonDeserializationContext.CreateRoot(jsonReader);
                return BsonDocumentSerializer.Instance.Deserialize(context);
            }
        }

        private static byte[] ToBytes(BsonDocument doc)
        {
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                return doc.ToBson(writerSettings: new BsonBinaryWriterSettings
                {
                    GuidRepresentation = GuidRepresentation.Unspecified
                });
            }
#pragma warning restore 618

            return doc.ToBson();
        }

        // nested classes
        private class AwsCredentials
        {
            public string Username;
            public SecureString Password;
            public string SessionToken;
        }

        private class AwsHttpClientHelper
        {
            // private static
            private static readonly Uri _ec2BaseUri = new Uri("http://169.254.169.254");
            private static readonly Uri _ecsBaseUri = new Uri("http://169.254.170.2");
            private static readonly Lazy<HttpClient> __httpClientInstance = new Lazy<HttpClient>(() => new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            });

            public static async Task<string> GetEC2Response()
            {
                var tokenRequest = CreateTokenRequest(_ec2BaseUri);
                var token = await GetHttpContent(tokenRequest, "Failed to acquire EC2 token.").ConfigureAwait(false);

                var roleRequest = CreateRoleRequest(_ec2BaseUri, token);
                var roleName = await GetHttpContent(roleRequest, "Failed to acquire EC2 role name.").ConfigureAwait(false);

                var credentialsRequest = CreateCredentialsRequest(_ec2BaseUri, roleName, token);
                var credentials = await GetHttpContent(credentialsRequest, "Failed to acquire EC2 credentials.").ConfigureAwait(false);

                return credentials;
            }

            public static async Task<string> GetECSResponse(string relativeUri)
            {
                var credentialsRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(_ecsBaseUri, relativeUri),
                    Method = HttpMethod.Get
                };

                return await GetHttpContent(credentialsRequest, "Failed to acquire ECS credentials.").ConfigureAwait(false);
            }

            // private static methods
            private static HttpRequestMessage CreateCredentialsRequest(Uri baseUri, string roleName, string token)
            {
                var credentialsUri = new Uri(baseUri, "latest/meta-data/iam/security-credentials/");
                var credentialsRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(credentialsUri, roleName),
                    Method = HttpMethod.Get
                };
                credentialsRequest.Headers.Add("X-aws-ec2-metadata-token", token);

                return credentialsRequest;
            }

            private static HttpRequestMessage CreateRoleRequest(Uri baseUri, string token)
            {
                var roleRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(baseUri, "latest/meta-data/iam/security-credentials/"),
                    Method = HttpMethod.Get
                };
                roleRequest.Headers.Add("X-aws-ec2-metadata-token", token);

                return roleRequest;
            }

            private static HttpRequestMessage CreateTokenRequest(Uri baseUri)
            {
                var tokenRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(baseUri, "latest/api/token"),
                    Method = HttpMethod.Put,
                };
                tokenRequest.Headers.Add("X-aws-ec2-metadata-token-ttl-seconds", "30");

                return tokenRequest;
            }

            private static async Task<string> GetHttpContent(HttpRequestMessage request, string exceptionMessage)
            {
                HttpResponseMessage response;
                try
                {
                    response = await __httpClientInstance.Value.SendAsync(request).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                }
                catch (OperationCanceledException ex)
                {
                    throw new MongoClientException(exceptionMessage, ex);
                }
                catch (HttpRequestException ex)
                {
                    throw new MongoClientException(exceptionMessage, ex);
                }

                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        private class MongoAWSMechanism : ISaslMechanism
        {
            private readonly UsernamePasswordCredential _credential;
            private readonly IClock _dateTimeProvider;
            private readonly IRandomByteGenerator _randomByteGenerator;
            private readonly string _sessionToken;

            public MongoAWSMechanism(
                UsernamePasswordCredential credential,
                string sessionToken,
                IRandomByteGenerator randomByteGenerator,
                IClock dateTimeProvider)
            {
                _credential = Ensure.IsNotNull(credential, nameof(credential));
                _sessionToken = sessionToken;
                _randomByteGenerator = Ensure.IsNotNull(randomByteGenerator, nameof(randomByteGenerator));
                _dateTimeProvider = Ensure.IsNotNull(dateTimeProvider, nameof(dateTimeProvider));
            }

            public string Name
            {
                get { return MechanismName; }
            }

            public ISaslStep Initialize(IConnection connection, SaslConversation conversation, ConnectionDescription description)
            {
                Ensure.IsNotNull(connection, nameof(connection));
                Ensure.IsNotNull(description, nameof(description));

                var nonce = GenerateRandomBytes();

                var document = new BsonDocument()
                    .Add("r", new BsonBinaryData(nonce))
                    .Add("p", new BsonInt32('n'));

                var clientFirstMessageBytes = ToBytes(document);

                return new ClientFirst(clientFirstMessageBytes, nonce, _credential, _sessionToken, _dateTimeProvider);
            }

            private byte[] GenerateRandomBytes()
            {
                return _randomByteGenerator.Generate(ClientNonceLength);
            }
        }

        private class ClientFirst : ISaslStep
        {
            private readonly byte[] _bytesToSendToServer;
            private readonly UsernamePasswordCredential _credential;
            private readonly IClock _dateTimeProvider;
            private readonly byte[] _nonce;
            private readonly string _sessionToken;

            public ClientFirst(
                byte[] bytesToSendToServer,
                byte[] nonce,
                UsernamePasswordCredential credential,
                string sessionToken,
                IClock dateTimeProvider)
            {
                _bytesToSendToServer = bytesToSendToServer;
                _credential = credential;
                _dateTimeProvider = dateTimeProvider;
                _nonce = nonce;
                _sessionToken = sessionToken;
            }

            public byte[] BytesToSendToServer
            {
                get { return _bytesToSendToServer; }
            }

            public bool IsComplete
            {
                get { return false; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                var serverFirstMessageDoc = ToDocument(bytesReceivedFromServer);
                var serverNonce = serverFirstMessageDoc["s"].AsByteArray;
                var host = serverFirstMessageDoc["h"].AsString;

                if (serverNonce.Length != ClientNonceLength * 2 || !serverNonce.Take(ClientNonceLength).SequenceEqual(_nonce))
                {
                    throw new MongoAuthenticationException(conversation.ConnectionId, message: "Server sent an invalid nonce.");
                }

                AwsSignatureVersion4.SignRequest(
                    _dateTimeProvider.UtcNow,
                    _credential.Username,
                    _credential.GetInsecurePassword(),
                    _sessionToken,
                    serverNonce,
                    host,
                    out var authHeader,
                    out var timestamp);

                var document = new BsonDocument()
                    .Add("a", authHeader)
                    .Add("d", timestamp);

                if (_sessionToken != null)
                {
                    document.Add("t", _sessionToken);
                }

                var clientSecondMessageBytes = ToBytes(document);

                return new ClientLast(clientSecondMessageBytes);
            }
        }

        private class ClientLast : ISaslStep
        {
            private readonly byte[] _bytesToSendToServer;

            public ClientLast(byte[] bytesToSendToServer)
            {
                _bytesToSendToServer = bytesToSendToServer;
            }

            public byte[] BytesToSendToServer
            {
                get { return _bytesToSendToServer; }
            }

            public bool IsComplete
            {
                get { return false; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                return new CompletedStep();
            }
        }
    }
}
