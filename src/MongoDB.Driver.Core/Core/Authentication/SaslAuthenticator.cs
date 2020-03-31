/* Copyright 2015-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// Base class for a SASL authenticator.
    /// </summary>
    public abstract class SaslAuthenticator : IAuthenticator
    {
        // fields
        private readonly ISaslMechanism _mechanism;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaslAuthenticator"/> class.
        /// </summary>
        /// <param name="mechanism">The mechanism.</param>
        protected SaslAuthenticator(ISaslMechanism mechanism)
        {
            _mechanism = Ensure.IsNotNull(mechanism, nameof(mechanism));
        }

        // properties
        /// <inheritdoc/>
        public string Name
        {
            get { return _mechanism.Name; }
        }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        /// <value>
        /// The name of the database.
        /// </value>
        public abstract string DatabaseName { get; }

        // methods
        /// <inheritdoc/>
        public void Authenticate(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));

            var firstStepResult = description.IsMasterResult.SpeculativeAuthenticate;

            if (firstStepResult == null)
            {
                Authenticate(connection, description, previousStep: null, previousStepResult: null, cancellationToken);
            }
            else
            {
                var (firstStep, _) = CreateFirstStepAndCommand(_mechanism, connection, conversation: null, description);
                Authenticate(
                    connection,
                    description,
                    previousStep: firstStep,
                    previousStepResult: firstStepResult,
                    cancellationToken);
            }
        }

        private void Authenticate(
            IConnection connection,
            ConnectionDescription description,
            ISaslStep previousStep,
            BsonDocument previousStepResult,
            CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));
            if (previousStep != null)
            {
                Ensure.IsNotNull(previousStepResult, nameof(previousStepResult));
            }

            using (var conversation = new SaslConversation(description.ConnectionId))
            {
                /* We cannot combine the two statements below into a single statement without adding a reference to
                 * ValueTuple */
                var currentStepAndCommand = previousStep == null
                    ? CreateFirstStepAndCommand(_mechanism, connection, conversation, description)
                    : CreateNextStepAndCommand(conversation, previousStep, previousStepResult);
                var (currentStep, command) = currentStepAndCommand;

                while (currentStep != null)
                {
                    BsonDocument result;
                    try
                    {
                        var protocol = CreateCommandProtocol(command);
                        result = protocol.Execute(connection, cancellationToken);
                    }
                    catch (MongoCommandException ex)
                    {
                        throw CreateException(connection, ex);
                    }
                    (currentStep, command) = CreateNextStepAndCommand(conversation, currentStep, result);
                }
            }
        }

        /// <inheritdoc/>
        public async Task AuthenticateAsync(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));

            var firstStepResult = description.IsMasterResult.SpeculativeAuthenticate;

            if (firstStepResult == null)
            {
                await AuthenticateAsync(
                        connection, description, previousStep: null, previousStepResult: null, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var (firstStep, _) = CreateFirstStepAndCommand(_mechanism, connection, conversation: null, description);
                await AuthenticateAsync(
                    connection,
                    description,
                    previousStep: firstStep,
                    previousStepResult: firstStepResult,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task AuthenticateAsync(
            IConnection connection,
            ConnectionDescription description,
            ISaslStep previousStep,
            BsonDocument previousStepResult,
            CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));
            if (previousStep != null)
            {
                Ensure.IsNotNull(previousStepResult, nameof(previousStepResult));
            }

            using (var conversation = new SaslConversation(description.ConnectionId))
            {
                /* We cannot combine the two statements below into a single statement without adding a reference to
                 * ValueTuple */
                var currentStepAndCommand = previousStep == null
                    ? CreateFirstStepAndCommand(_mechanism, connection, conversation, description)
                    : CreateNextStepAndCommand(conversation, previousStep, previousStepResult);
                var (currentStep, command) = currentStepAndCommand;
                while (currentStep != null)
                {
                    BsonDocument result;
                    try
                    {
                        var protocol = CreateCommandProtocol(command);
                        result = await protocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
                    }
                    catch (MongoCommandException ex)
                    {
                        throw CreateException(connection, ex);
                    }
                    (currentStep, command) = CreateNextStepAndCommand(conversation, currentStep, result);
                }
            }
        }

        /// <inheritdoc/>
        public BsonDocument CustomizeInitialIsMasterCommand(BsonDocument isMasterCommand)
        {
            switch (_mechanism.Name)
            {
                case "SCRAM-SHA-1":
                case "SCRAM-SHA-256":
                    (var firstStep, _) = CreateFirstStepAndCommand(_mechanism, null, null, null);
                    isMasterCommand.Add("speculativeAuthenticate", CreateStartCommand(firstStep));
                    break;
            }

            return isMasterCommand;
        }

        private CommandWireProtocol<BsonDocument> CreateCommandProtocol(BsonDocument command)
        {
            return new CommandWireProtocol<BsonDocument>(
                new DatabaseNamespace(DatabaseName),
                command,
                true,
                BsonDocumentSerializer.Instance,
                null);
        }

        private BsonDocument CreateContinueCommand(ISaslStep currentStep, BsonDocument result)
        {
            return new BsonDocument
            {
                { "saslContinue", 1 },
                { "conversationId", result["conversationId"].AsInt32 },
                { "payload", currentStep.BytesToSendToServer }
            };
        }

        private MongoAuthenticationException CreateException(IConnection connection, Exception ex)
        {
            var message = string.Format("Unable to authenticate using sasl protocol mechanism {0}.", Name);
            return new MongoAuthenticationException(connection.ConnectionId, message, ex);
        }

        private StepAndCommand CreateFirstStepAndCommand(
            ISaslMechanism mechanism,
            IConnection connection,
            SaslConversation conversation,
            ConnectionDescription description)
        {
            var currentStep =  mechanism.Initialize(connection, conversation, description);
            var command = CreateStartCommand(currentStep);
            return new StepAndCommand(currentStep, command);
        }

        private StepAndCommand CreateNextStepAndCommand(
            SaslConversation conversation,
            ISaslStep currentStep,
            BsonDocument result)
        {
            currentStep = Transition(conversation, currentStep, result);
            return currentStep == null
                ? new StepAndCommand(null, null)
                : new StepAndCommand(currentStep, CreateContinueCommand(currentStep, result));
        }

        private BsonDocument CreateStartCommand(ISaslStep currentStep)
        {
            var startCommand = new BsonDocument
            {
                { "saslStart", 1 },
                { "mechanism", _mechanism.Name },
                { "payload", currentStep.BytesToSendToServer }
            };

            if (_mechanism.Name.StartsWith("SCRAM", StringComparison.OrdinalIgnoreCase))
            {
                startCommand.Add("options", new BsonDocument("skipEmptyExchange", true));
            }

            return startCommand;
        }

        private ISaslStep Transition(SaslConversation conversation, ISaslStep currentStep, BsonDocument result)
        {
            // we might be done here if the client is not expecting a reply from the server
            if (result.GetValue("done", false).ToBoolean() && currentStep.IsComplete)
            {
                return null;
            }

            currentStep = currentStep.Transition(conversation, result["payload"].AsByteArray);

            // we might be done here if the client had some final verification it needed to do
            if (result.GetValue("done", false).ToBoolean() && currentStep.IsComplete)
            {
                return null;
            }

            return currentStep;
        }

        // nested classes
        /// <summary>
        /// Represents a SASL conversation.
        /// </summary>
        protected sealed class SaslConversation : IDisposable
        {
            // fields
            private readonly ConnectionId _connectionId;
            private List<IDisposable> _itemsNeedingDisposal;
            private bool _isDisposed;

            // constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="SaslConversation"/> class.
            /// </summary>
            /// <param name="connectionId">The connection identifier.</param>
            public SaslConversation(ConnectionId connectionId)
            {
                _connectionId = connectionId;
                _itemsNeedingDisposal = new List<IDisposable>();
            }

            // properties
            /// <summary>
            /// Gets the connection identifier.
            /// </summary>
            /// <value>
            /// The connection identifier.
            /// </value>
            public ConnectionId ConnectionId
            {
                get { return _connectionId; }
            }

            // methods
            /// <inheritdoc/>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Registers the item for disposal.
            /// </summary>
            /// <param name="item">The disposable item.</param>
            public void RegisterItemForDisposal(IDisposable item)
            {
                _itemsNeedingDisposal.Add(item);
            }

            private void Dispose(bool disposing)
            {
                if (!_isDisposed)
                {
                    // disposal should happen in reverse order of registration.
                    if (disposing && _itemsNeedingDisposal != null)
                    {
                        for (int i = _itemsNeedingDisposal.Count - 1; i >= 0; i--)
                        {
                            _itemsNeedingDisposal[i].Dispose();
                        }

                        _itemsNeedingDisposal.Clear();
                        _itemsNeedingDisposal = null;
                    }

                    _isDisposed = true;
                }
            }
        }

        /// <summary>
        /// Represents a SASL mechanism.
        /// </summary>
        protected interface ISaslMechanism
        {
            // properties
            /// <summary>
            /// Gets the name of the mechanism.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            string Name { get; }

            // methods
            /// <summary>
            /// Initializes the mechanism.
            /// </summary>
            /// <param name="connection">The connection.</param>
            /// <param name="conversation">The SASL conversation.</param>
            /// <param name="description">The connection description.</param>
            /// <returns>The initial SASL step.</returns>
            ISaslStep Initialize(IConnection connection, SaslConversation conversation, ConnectionDescription description);
        }

        /// <summary>
        /// Represents a SASL step.
        /// </summary>
        protected interface ISaslStep
        {
            // properties
            /// <summary>
            /// Gets the bytes to send to server.
            /// </summary>
            /// <value>
            /// The bytes to send to server.
            /// </value>
            byte[] BytesToSendToServer { get; }

            /// <summary>
            /// Gets a value indicating whether this instance is complete.
            /// </summary>
            /// <value>
            /// <c>true</c> if this instance is complete; otherwise, <c>false</c>.
            /// </value>
            bool IsComplete { get; }

            // methods
            /// <summary>
            /// Transitions the SASL conversation to the next step.
            /// </summary>
            /// <param name="conversation">The SASL conversation.</param>
            /// <param name="bytesReceivedFromServer">The bytes received from server.</param>
            /// <returns>The next SASL step.</returns>
            ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer);
        }

        /// <summary>
        /// Represents a completed SASL step.
        /// </summary>
        protected class CompletedStep : ISaslStep
        {
            // fields
            private readonly byte[] _bytesToSendToServer;

            // constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="CompletedStep"/> class.
            /// </summary>
            public CompletedStep()
                : this(new byte[0])
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="CompletedStep"/> class.
            /// </summary>
            /// <param name="bytesToSendToServer">The bytes to send to server.</param>
            public CompletedStep(byte[] bytesToSendToServer)
            {
                _bytesToSendToServer = bytesToSendToServer;
            }

            // properties
            /// <inheritdoc/>
            public byte[] BytesToSendToServer
            {
                get { return _bytesToSendToServer; }
            }

            /// <inheritdoc/>
            public bool IsComplete
            {
                get { return true; }
            }

            /// <inheritdoc/>
            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                throw new InvalidOperationException("Sasl conversation has completed.");
            }
        }

        private readonly struct StepAndCommand
        {
            private readonly BsonDocument _command;
            private readonly ISaslStep _step;

            public BsonDocument Command => _command;

            public ISaslStep Step => _step;

            public StepAndCommand(ISaslStep step, BsonDocument command)
            {
                _step = step;
                _command = command;
            }

            public void Deconstruct(out ISaslStep step, out BsonDocument command)
            {
                step = Step;
                command = Command;
            }
        }
    }
}
