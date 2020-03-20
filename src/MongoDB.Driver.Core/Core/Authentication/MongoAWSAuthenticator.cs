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
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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
        /// Initializes a new instance of the <see cref="MongoAWSAuthenticator"/> class.
        /// </summary>
        /// <param name="credential">The credentials.</param>
        /// <param name="properties">The properties.</param>
        public MongoAWSAuthenticator(UsernamePasswordCredential credential, IEnumerable<KeyValuePair<string, string>> properties)
            : this(credential, properties, new DefaultRandomByteGenerator(), SystemClock.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoAWSAuthenticator"/> class.
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
            IClock clock)
            : base(CreateMechanism(credential, properties, randomByteGenerator, clock))
        {
        }

        internal MongoAWSAuthenticator(
            string username,
            IEnumerable<KeyValuePair<string, string>> properties,
            IRandomByteGenerator randomByteGenerator,
            IClock clock)
            : base(CreateMechanism(username, null, properties, randomByteGenerator, clock))
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
            IClock clock)
        {
            if (credential.Source != "$external")
            {
                throw new ArgumentException("MONGODB-AWS authentication may only use the $external source.", nameof(credential));
            }

            return CreateMechanism(credential.Username, credential.Password, properties, randomByteGenerator, clock);
        }

        private static MongoAWSMechanism CreateMechanism(
            string username,
            SecureString securePassword,
            IEnumerable<KeyValuePair<string, string>> properties,
            IRandomByteGenerator randomByteGenerator,
            IClock clock)
        {
            ValidateMechanismProperties(properties);

            var awsCredentials =
                CreateAwsCredentialsFromMongoCredentials(username, securePassword, properties) ??
                CreateAwsCredentialsFromEnvironmentVariables() ??
                CreateAwsCredentialsFromEcsResponse() ??
                CreateAwsCredentialsFromEc2Response();

            if (awsCredentials == null)
            {
                throw new InvalidOperationException("A MONGODB-AWS credential must have an access key ID.");
            }

            var credentials = new UsernamePasswordCredential("$external", awsCredentials.Username, awsCredentials.Password);

            return new MongoAWSMechanism(credentials, awsCredentials.SessionToken, randomByteGenerator, clock);
        }

        private static AwsCredentials CreateAwsCredentialsFromMongoCredentials(string username, SecureString securePassword, IEnumerable<KeyValuePair<string, string>> properties)
        {
            var sessionToken = ExtractSessionTokenFromMechanismProperties(properties);

            var awsCredentials = new AwsCredentials
            {
                Username = username,
                Password = securePassword,
                SessionToken = sessionToken
            };

            ValidateAwsCredentials(awsCredentials);

            if (awsCredentials.Username == null)
            {
                return null;
            }
            
            return awsCredentials;
        }

        private static AwsCredentials CreateAwsCredentialsFromEnvironmentVariables()
        {
            var username = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var password = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            var sessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");

            var awsCredentials = new AwsCredentials
            {
                Username = username,
                Password = ToSecureString(password),
                SessionToken = sessionToken
            };

            ValidateAwsCredentials(awsCredentials);

            if (awsCredentials.Username == null)
            {
                return null;
            }

            return awsCredentials;
        }

        private static AwsCredentials CreateAwsCredentialsFromEcsResponse()
        {
            var relativeUri = Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_RELATIVE_URI");

            if (relativeUri == null)
            {
                return null;
            }

            var response = AwsHttpClientHelper.GetECSResponseAsync(relativeUri).GetAwaiter().GetResult();
            var parsedResponse = BsonDocument.Parse(response);
            var username = parsedResponse.GetValue("AccessKeyId", null)?.AsString;
            var password = parsedResponse.GetValue("SecretAccessKey", null)?.AsString;
            var sessionToken = parsedResponse.GetValue("Token", null)?.AsString;

            var awsCredentials = new AwsCredentials
            {
                Username = username,
                Password = ToSecureString(password),
                SessionToken = sessionToken
            };

            ValidateAwsCredentials(awsCredentials);

            if (awsCredentials.Username == null)
            {
                return null;
            }

            return awsCredentials;
        }

        private static AwsCredentials CreateAwsCredentialsFromEc2Response()
        {
            var response = AwsHttpClientHelper.GetEC2ResponseAsync().GetAwaiter().GetResult();
            var parsedResponse = BsonDocument.Parse(response);
            var username = parsedResponse.GetValue("AccessKeyId", null)?.AsString;
            var password = parsedResponse.GetValue("SecretAccessKey", null)?.AsString;
            var sessionToken = parsedResponse.GetValue("Token", null)?.AsString;

            var awsCredentials = new AwsCredentials
            {
                Username = username,
                Password = ToSecureString(password),
                SessionToken = sessionToken
            };

            ValidateAwsCredentials(awsCredentials);

            if (awsCredentials.Username == null)
            {
                return null;
            }

            return awsCredentials;
        }

        private static string ExtractSessionTokenFromMechanismProperties(IEnumerable<KeyValuePair<string, string>> properties)
        {
            if (properties == null)
            {
                return null;
            }

            foreach (var pair in properties)
            {
                if (pair.Key.ToUpperInvariant() == "AWS_SESSION_TOKEN")
                {
                    return pair.Value;
                }
            }

            return null;
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
                throw new InvalidOperationException("A MONGODB-AWS credential must have an access key ID.");
            }
            if (awsCredentials.Username != null && awsCredentials.Password == null)
            {
                throw new InvalidOperationException("A MONGODB-AWS credential must have a secret access key.");
            }
        }

        private static void ValidateMechanismProperties(IEnumerable<KeyValuePair<string, string>> properties)
        {
            if (properties == null)
            {
                return;
            }

            foreach (var pair in properties)
            {
                if (pair.Key.ToUpperInvariant() != "AWS_SESSION_TOKEN")
                {
                    throw new ArgumentException($"Unknown AWS property '{pair.Key}'.", nameof(properties));
                }
            }
        }

        // nested classes
        private class AwsCredentials
        {
            public string Username;
            public SecureString Password;
            public string SessionToken;
        }

        private static class AwsHttpClientHelper
        {
            // private static
            private static readonly Uri __ec2BaseUri = new Uri("http://169.254.169.254");
            private static readonly Uri __ecsBaseUri = new Uri("http://169.254.170.2");
            private static readonly Lazy<HttpClient> __httpClientInstance = new Lazy<HttpClient>(() => new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            });

            public static async Task<string> GetEC2ResponseAsync()
            {
                var tokenRequest = CreateTokenRequest(__ec2BaseUri);
                var token = await GetHttpContentAsync(tokenRequest, "Failed to acquire EC2 token.").ConfigureAwait(false);

                var roleRequest = CreateRoleRequest(__ec2BaseUri, token);
                var roleName = await GetHttpContentAsync(roleRequest, "Failed to acquire EC2 role name.").ConfigureAwait(false);

                var credentialsRequest = CreateCredentialsRequest(__ec2BaseUri, roleName, token);
                var credentials = await GetHttpContentAsync(credentialsRequest, "Failed to acquire EC2 credentials.").ConfigureAwait(false);

                return credentials;
            }

            public static async Task<string> GetECSResponseAsync(string relativeUri)
            {
                var credentialsRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(__ecsBaseUri, relativeUri),
                    Method = HttpMethod.Get
                };

                return await GetHttpContentAsync(credentialsRequest, "Failed to acquire ECS credentials.").ConfigureAwait(false);
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

            private static async Task<string> GetHttpContentAsync(HttpRequestMessage request, string exceptionMessage)
            {
                HttpResponseMessage response;
                try
                {
                    response = await __httpClientInstance.Value.SendAsync(request).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex) when (ex is OperationCanceledException || ex is MongoClientException)
                {
                    throw new MongoClientException(exceptionMessage, ex);
                }

                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        private class MongoAWSMechanism : ISaslMechanism
        {
            private readonly IClock _clock;
            private readonly UsernamePasswordCredential _credential;
            private readonly IRandomByteGenerator _randomByteGenerator;
            private readonly string _sessionToken;

            public MongoAWSMechanism(
                UsernamePasswordCredential credential,
                string sessionToken,
                IRandomByteGenerator randomByteGenerator,
                IClock clock)
            {
                _credential = Ensure.IsNotNull(credential, nameof(credential));
                _sessionToken = sessionToken;
                _randomByteGenerator = Ensure.IsNotNull(randomByteGenerator, nameof(randomByteGenerator));
                _clock = Ensure.IsNotNull(clock, nameof(clock));
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

                var document = new BsonDocument
                {
                    { "r", nonce },
                    { "p", (int)'n' }
                };

                var clientMessageBytes = document.ToBson();

                return new ClientFirst(clientMessageBytes, nonce, _credential, _sessionToken, _clock);
            }

            private byte[] GenerateRandomBytes()
            {
                return _randomByteGenerator.Generate(ClientNonceLength);
            }
        }

        private class ClientFirst : ISaslStep
        {
            private readonly byte[] _bytesToSendToServer;
            private readonly IClock _clock;
            private readonly UsernamePasswordCredential _credential;
            private readonly byte[] _nonce;
            private readonly string _sessionToken;

            public ClientFirst(
                byte[] bytesToSendToServer,
                byte[] nonce,
                UsernamePasswordCredential credential,
                string sessionToken,
                IClock clock)
            {
                _bytesToSendToServer = bytesToSendToServer;
                _clock = clock;
                _credential = credential;
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
                var serverFirstMessageDocument = BsonSerializer.Deserialize<BsonDocument>(bytesReceivedFromServer);
                var serverNonce = serverFirstMessageDocument["s"].AsByteArray;
                var host = serverFirstMessageDocument["h"].AsString;

                if (serverNonce.Length != ClientNonceLength * 2 || !serverNonce.Take(ClientNonceLength).SequenceEqual(_nonce))
                {
                    throw new MongoAuthenticationException(conversation.ConnectionId, "Server sent an invalid nonce.");
                }
                if (host.Length < 1 || host.Length > 255 || host.Contains(".."))
                {
                    throw new MongoAuthenticationException(conversation.ConnectionId, "Server returned an invalid sts host.");
                }

                AwsSignatureVersion4.CreateAuthorizationRequest(
                    _clock.UtcNow,
                    _credential.Username,
                    _credential.GetInsecurePassword(),
                    _sessionToken,
                    serverNonce,
                    host,
                    out var authorizationHeader,
                    out var timestamp);

                var document = new BsonDocument
                {
                    { "a", authorizationHeader },
                    { "d", timestamp },
                    { "t",  _sessionToken, _sessionToken != null }
                };

                var clientSecondMessageBytes = document.ToBson();

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
