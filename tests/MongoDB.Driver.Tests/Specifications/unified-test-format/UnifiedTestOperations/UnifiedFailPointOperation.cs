﻿/* Copyright 2020-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.TestHelpers;

namespace MongoDB.Driver.Tests.Specifications.unified_test_format.UnifiedTestOperations
{
    public class UnifiedFailPointOperation : IUnifiedTestOperation
    {
        private readonly IMongoClient _client;
        private readonly BsonDocument _failPointCommand;
        private FailPoint _failPoint;

        public UnifiedFailPointOperation(
            IMongoClient client,
            BsonDocument failPointCommand)
        {
            _client = client;
            _failPointCommand = failPointCommand;
        }

        public FailPoint FailPoint => _failPoint;

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            var cluster = _client.Cluster;
            var session = NoCoreSession.NewHandle(); // TODO: Check if session should be specified
            // TODO: Spec requires "primary" read preference
            _failPoint = FailPoint.Configure(cluster, session, _failPointCommand);

            return null;
        }

        public Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute(cancellationToken));
        }
    }

    public class UnifiedFailPointOperationBuilder
    {
        private EntityMap _entityMap;

        public UnifiedFailPointOperationBuilder(EntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedFailPointOperation Build(BsonDocument arguments)
        {
            IMongoClient client = null;
            BsonDocument command = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "client":
                        client = _entityMap.GetClient(argument.Value.AsString);
                        break;
                    case "failPoint":
                        command = argument.Value.AsBsonDocument;
                        break;
                    default:
                        throw new FormatException($"Invalid FailPointOperation argument name: {argument.Name}");
                }
            }

            return new UnifiedFailPointOperation(client, command);
        }
    }

}
