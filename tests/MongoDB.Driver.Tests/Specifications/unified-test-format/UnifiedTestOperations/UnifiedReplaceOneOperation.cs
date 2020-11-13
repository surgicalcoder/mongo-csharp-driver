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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.unified_test_format.UnifiedTestOperations
{
    public class UnifiedReplaceOneOperation : IUnifiedTestOperation
    {
        private IMongoCollection<BsonDocument> _collection;
        private BsonDocument _replacement;
        private FilterDefinition<BsonDocument> _filter;
        private ReplaceOptions _options;

        public UnifiedReplaceOneOperation(
            IMongoCollection<BsonDocument> collection,
            BsonDocument replacement,
            FilterDefinition<BsonDocument> filter,
            ReplaceOptions options)
        {
            _collection = collection;
            _replacement = replacement;
            _filter = filter;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            ReplaceOneResult result;

            try
            {
                result = _collection.ReplaceOne(_filter, _replacement, _options, cancellationToken);
            }
            catch (Exception ex)
            {
                return new OperationResult(ex);
            }

            return new UnifiedReplaceOneOperationResultConverter().Convert(result);
        }

        public async Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            ReplaceOneResult result;

            try
            {
                result = await _collection.ReplaceOneAsync(_filter, _replacement, _options, cancellationToken);
            }
            catch (Exception ex)
            {
                return new OperationResult(ex);
            }

            return new UnifiedReplaceOneOperationResultConverter().Convert(result);
        }
    }

    public class UnifiedReplaceOneOperationBuilder
    {
        private readonly EntityMap _entityMap;

        public UnifiedReplaceOneOperationBuilder(EntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedReplaceOneOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.GetCollection(targetCollectionId);

            FilterDefinition<BsonDocument> filter = null;
            BsonDocument replacement = null;
            ReplaceOptions options = new ReplaceOptions();

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "replacement":
                        replacement = argument.Value.AsBsonDocument;
                        break;
                    case "upsert":
                        options.IsUpsert = argument.Value.AsBoolean;
                        break;
                    default:
                        throw new FormatException($"Invalid ReplaceOneOperation argument name: {argument.Name}");
                }
            }

            return new UnifiedReplaceOneOperation(collection, replacement, filter, options);
        }
    }

    public class UnifiedReplaceOneOperationResultConverter
    {
        public OperationResult Convert(ReplaceOneResult result)
        {
            throw new NotImplementedException("Specification requirements are not clear on result format");
        }
    }
}
