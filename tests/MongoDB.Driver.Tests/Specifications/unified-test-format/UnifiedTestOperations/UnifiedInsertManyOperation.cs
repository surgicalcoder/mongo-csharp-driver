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
    public class UnifiedInsertManyOperation : IUnifiedTestOperation
    {
        private IMongoCollection<BsonDocument> _collection;
        private List<BsonDocument> _documents;
        private InsertManyOptions _options;
        private IClientSessionHandle _session;

        public UnifiedInsertManyOperation(
            IMongoCollection<BsonDocument> collection,
            List<BsonDocument> documents,
            InsertManyOptions options,
            IClientSessionHandle session)
        {
            _collection = collection;
            _documents = documents;
            _options = options; // TODO: should not be null. Either throw or recreate
            _session = session;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                if (_session == null)
                {
                    _collection.InsertMany(_documents, _options, cancellationToken);
                }
                else
                {
                    _collection.InsertMany(_session, _documents, _options, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                return new OperationResult(ex);
            }

            return null;
        }

        public async Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_session == null)
                {
                    await _collection.InsertManyAsync(_documents, _options, cancellationToken);
                }
                else
                {
                    await _collection.InsertManyAsync(_session, _documents, _options, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                return new OperationResult(ex);
            }

            return null;
        }
    }

    public class UnifiedInsertManyOperationBuilder
    {
        private EntityMap _entityMap;

        public UnifiedInsertManyOperationBuilder(EntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedInsertManyOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            IMongoCollection<BsonDocument> collection = _entityMap.GetCollection(targetCollectionId);

            List<BsonDocument> documents = null;
            InsertManyOptions options = new InsertManyOptions();
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "ordered":
                        options.IsOrdered = argument.Value.AsBoolean;
                        break;
                    case "documents":
                        documents = argument.Value.AsBsonArray.Cast<BsonDocument>().ToList();
                        break;
                    case "session":
                        session = _entityMap.GetSession(argument.Value.AsString);
                        break;
                    default:
                        throw new FormatException($"Invalid InsertManyOperation argument name: {argument.Name}");
                }
            }

            return new UnifiedInsertManyOperation(collection, documents, options, session);
        }
    }
}
