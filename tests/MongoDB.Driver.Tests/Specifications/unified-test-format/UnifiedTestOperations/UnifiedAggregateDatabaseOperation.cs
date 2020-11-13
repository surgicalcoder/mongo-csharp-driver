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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.unified_test_format.UnifiedTestOperations
{
    public class UnifiedAggregateDatabaseOperation : IUnifiedTestOperation
    {
        private IMongoDatabase _database;
        private AggregateOptions _options;
        private PipelineDefinition<NoPipelineInput, BsonDocument> _pipeline;

        public UnifiedAggregateDatabaseOperation(
            IMongoDatabase database,
            AggregateOptions options,
            PipelineDefinition<NoPipelineInput, BsonDocument> pipeline)
        {
            _database = database;
            _options = options;
            _pipeline = pipeline;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            List<BsonDocument> result;

            try
            {
                var cursor = _database.Aggregate(_pipeline, _options, cancellationToken);
                result = cursor.ToList();
            }
            catch (Exception ex)
            {
                return new OperationResult(ex);
            }

            return new OperationResult(new BsonArray(result));
        }

        public async Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            List<BsonDocument> result;

            try
            {
                var cursor = await _database.AggregateAsync(_pipeline, _options, cancellationToken);
                result = cursor.ToList();
            }
            catch (Exception ex)
            {
                return new OperationResult(ex);
            }

            return new OperationResult(new BsonArray(result));
        }
    }

    public class UnifiedAggregateDatabaseOperationBuilder
    {
        private readonly EntityMap _entityMap;

        public UnifiedAggregateDatabaseOperationBuilder(EntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedAggregateDatabaseOperation Build(string targetDatabaseId, BsonDocument arguments)
        {
            var collection = _entityMap.GetDatabase(targetDatabaseId);

            AggregateOptions options = new AggregateOptions();
            PipelineDefinition<NoPipelineInput, BsonDocument> pipeline = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "pipeline":
                        pipeline = new BsonDocumentStagePipelineDefinition<NoPipelineInput, BsonDocument>(
                            argument.Value.AsBsonArray.Cast<BsonDocument>());
                        break;
                    default:
                        throw new FormatException($"Invalid AggregateOperation argument name: {argument.Name}");
                }
            }

            return new UnifiedAggregateDatabaseOperation(collection, options, pipeline);
        }
    }
}
