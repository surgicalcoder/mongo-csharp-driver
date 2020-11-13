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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.unified_test_format.UnifiedTestOperations
{
    public class UnifiedFindOperation : IUnifiedTestOperation
    {
        private IMongoCollection<BsonDocument> _collection;
        private FilterDefinition<BsonDocument> _filter;
        private FindOptions<BsonDocument> _options;

        public UnifiedFindOperation(
            IMongoCollection<BsonDocument> collection,
            FilterDefinition<BsonDocument> filter,
            FindOptions<BsonDocument> options)
        {
            _collection = collection;
            _filter = filter;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            List<BsonDocument> result;

            try
            {
                var cursor = _collection.FindSync(_filter, _options, cancellationToken);
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
                var cursor = await _collection.FindAsync(_filter, _options, cancellationToken);
                result = cursor.ToList();
            }
            catch (Exception ex)
            {
                return new OperationResult(ex);
            }

            return new OperationResult(new BsonArray(result));
        }
    }

    public class UnifiedFindOperationBuilder
    {
        private readonly EntityMap _entityMap;

        public UnifiedFindOperationBuilder(EntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedFindOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.GetCollection(targetCollectionId);

            FilterDefinition<BsonDocument> filter = null;
            FindOptions<BsonDocument> options = new FindOptions<BsonDocument>();

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "batchSize":
                        options.BatchSize = argument.Value.AsInt32;
                        break;
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "limit":
                        options.Limit = argument.Value.AsInt32;
                        break;
                    case "sort":
                        options.Sort = (SortDefinition<BsonDocument>)argument.Value;
                        break;
                    default:
                        throw new FormatException($"Invalid FindOperation argument name: {argument.Name}");
                }
            }

            return new UnifiedFindOperation(collection, filter, options);
        }
    }
}
