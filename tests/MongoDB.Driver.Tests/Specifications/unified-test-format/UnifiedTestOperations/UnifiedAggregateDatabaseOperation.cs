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
            PipelineDefinition<NoPipelineInput, BsonDocument> pipeline,
            AggregateOptions options)
        {
            _database = database;
            _pipeline = pipeline;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var cursor = _database.Aggregate(_pipeline, _options, cancellationToken);
                var result = cursor.ToList();
                return new OperationResult(new BsonArray(result));
            }
            catch (Exception exception)
            {
                return new OperationResult(exception);
            }
        }

        public async Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                var cursor = await _database.AggregateAsync(_pipeline, _options, cancellationToken);
                var result = await cursor.ToListAsync();
                return new OperationResult(new BsonArray(result));
            }
            catch (Exception exception)
            {
                return new OperationResult(exception);
            }
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
            // TODO: Check if session should be processed

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "pipeline":
                        var stages = argument.Value.AsBsonArray.Cast<BsonDocument>();
                        pipeline = new BsonDocumentStagePipelineDefinition<NoPipelineInput, BsonDocument>(stages);
                        break;
                    default:
                        throw new FormatException($"Invalid AggregateOperation argument name: {argument.Name}");
                }
            }

            return new UnifiedAggregateDatabaseOperation(collection, pipeline, options);
        }
    }
}
