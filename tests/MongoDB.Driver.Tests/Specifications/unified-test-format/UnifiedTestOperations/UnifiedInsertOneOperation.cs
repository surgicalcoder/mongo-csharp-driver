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
    public class UnifiedInsertOneOperation : IUnifiedTestOperation
    {
        private IMongoCollection<BsonDocument> _collection;
        private BsonDocument _document;
        private InsertOneOptions _options;
        private IClientSessionHandle _session;

        public UnifiedInsertOneOperation(
            IMongoCollection<BsonDocument> collection,
            BsonDocument document,
            InsertOneOptions options,
            IClientSessionHandle session)
        {
            _collection = collection;
            _document = document;
            _options = options; // TODO: should not be null. Either throw or recreate
            _session = session;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                if (_session == null)
                {
                    _collection.InsertOne(_document, _options, cancellationToken);
                }
                else
                {
                    _collection.InsertOne(_session, _document, _options, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                return new OperationResult(ex);
            }

            return new OperationResult((BsonDocument)null);
        }

        public async Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_session == null)
                {
                    await _collection.InsertOneAsync(_document, _options, cancellationToken);
                }
                else
                {
                    await _collection.InsertOneAsync(_session, _document, _options, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                return new OperationResult(ex);
            }

            return new OperationResult((BsonDocument)null);
        }
    }

    public class UnifiedInsertOneOperationBuilder
    {
        private EntityMap _entityMap;

        public UnifiedInsertOneOperationBuilder(EntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedInsertOneOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            IMongoCollection<BsonDocument> collection = _entityMap.GetCollection(targetCollectionId);

            BsonDocument document = null;
            InsertOneOptions options = new InsertOneOptions();
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "bypassDocumentValidation":
                        options.BypassDocumentValidation = argument.Value.AsBoolean;
                        break;
                    case "document":
                        document = argument.Value.AsBsonDocument;
                        break;
                    case "session":
                        session = _entityMap.GetSession(argument.Value.AsString);
                        break;
                    default:
                        throw new FormatException($"Invalid InsertOneOperation argument name: {argument.Name}"); // TODO: should I place dots at the end? Maybe quotes?
                }
            }

            return new UnifiedInsertOneOperation(collection, document, options, session);
        }
    }
}
