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
using MongoDB.Bson;
using MongoDB.Driver.Tests.Specifications.unified_test_format.UnifiedTestOperations;

namespace MongoDB.Driver.Tests.Specifications.unified_test_format
{
    public class UnifiedTestOperationFactory
    {
        private EntityMap _entityMap;

        public UnifiedTestOperationFactory(EntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public IUnifiedTestOperation CreateOperation(string operationName, string targetEntityId, BsonDocument operationArguments)
        {
            switch (targetEntityId)
            {
                case "testRunner":
                    switch (operationName)
                    {
                        case "assertCollectionExists":
                            throw new NotImplementedException();
                        case "assertCollectionNotExists":
                            throw new NotImplementedException();
                        case "assertDifferentLsidOnLastTwoCommands":
                            throw new NotImplementedException();
                        case "assertIndexExists":
                            throw new NotImplementedException();
                        case "assertIndexNotExists":
                            throw new NotImplementedException();
                        case "assertSessionDirty":
                            throw new NotImplementedException();
                        case "assertSessionNotDirty":
                            throw new NotImplementedException();
                        case "assertSessionPinned":
                            throw new NotImplementedException();
                        case "assertSessionUnpinned":
                            throw new NotImplementedException();
                        case "assertSameLsidOnLastTwoCommands":
                            throw new NotImplementedException();
                        case "assertSessionTransactionState":
                            throw new NotImplementedException();
                        case "assertEventCount":
                            throw new NotImplementedException();
                        case "configureFailPoint":
                            throw new NotImplementedException();
                        case "failPoint":
                            return new UnifiedFailPointOperationBuilder(_entityMap).Build(operationArguments);
                        case "recordPrimary":
                            throw new NotImplementedException();
                        case "runAdminCommand":
                            throw new NotImplementedException();
                        case "runOnThread":
                            throw new NotImplementedException();
                        case "startThread":
                            throw new NotImplementedException();
                        case "targetedFailPoint":
                            throw new NotImplementedException();
                        case "wait":
                            throw new NotImplementedException();
                        case "waitForEvent":
                            throw new NotImplementedException();
                        case "waitForPrimaryChange":
                            throw new NotImplementedException();
                        case "waitForThread":
                            throw new NotImplementedException();
                        default:
                            throw new FormatException($"Invalid method name: \"{operationName}\".");
                    }

                case var _ when targetEntityId.StartsWith("client"):
                    switch (operationName)
                    {
                        case "listDatabaseNames":
                            throw new NotImplementedException();
                        case "listDatabases":
                            return new UnifiedListDatabasesOperation(_entityMap.GetClient(targetEntityId));
                        case "watch":
                            throw new NotImplementedException();
                        default:
                            throw new FormatException($"Invalid method name: \"{operationName}\".");
                    }

                case var _ when targetEntityId.StartsWith("session"):
                    switch (operationName)
                    {
                        case "abortTransaction":
                            throw new NotImplementedException();
                        case "commitTransaction":
                            throw new NotImplementedException();
                        case "endSession":
                            throw new NotImplementedException();
                        case "startTransaction":
                            throw new NotImplementedException();
                        case "withTransaction":
                            throw new NotImplementedException();
                        default:
                            throw new FormatException($"Invalid method name: \"{operationName}\".");
                    }

                case var _ when targetEntityId.StartsWith("database"):
                    switch (operationName)
                    {
                        case "aggregate":
                            return new UnifiedAggregateDatabaseOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "createCollection":
                            throw new NotImplementedException();
                        case "dropCollection":
                            throw new NotImplementedException();
                        case "listCollectionNames":
                            throw new NotImplementedException();
                        case "listCollections":
                            throw new NotImplementedException();
                        case "runCommand":
                            throw new NotImplementedException();
                        case "watch":
                            throw new NotImplementedException();
                        default:
                            throw new FormatException($"Invalid method name: \"{operationName}\".");
                    }

                case var _ when targetEntityId.StartsWith("collection"):
                    switch (operationName)
                    {
                        case "aggregate":
                            return new UnifiedAggregateCollectionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "bulkWrite":
                            return new UnifiedBulkWriteOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "count":
                            throw new NotImplementedException();
                        case "countDocuments":
                            throw new NotImplementedException();
                        case "createIndex":
                            throw new NotImplementedException();
                        case "deleteMany":
                            throw new NotImplementedException();
                        case "deleteOne":
                            throw new NotImplementedException();
                        case "distinct":
                            throw new NotImplementedException();
                        case "doesNotExist":
                            throw new NotImplementedException();
                        case "dropIndex":
                            throw new NotImplementedException();
                        case "estimatedDocumentCount":
                            throw new NotImplementedException();
                        case "find":
                            return new UnifiedFindOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "findOne":
                            throw new NotImplementedException();
                        case "findOneAndDelete":
                            throw new NotImplementedException();
                        case "findOneAndReplace":
                            throw new NotImplementedException();
                        case "findOneAndUpdate":
                            throw new NotImplementedException();
                        case "insertMany":
                            return new UnifiedInsertManyOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "insertOne":
                            return new UnifiedInsertOneOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "listIndexes":
                            throw new NotImplementedException();
                        case "mapReduce":
                            throw new NotImplementedException();
                        case "replaceOne":
                            return new UnifiedReplaceOneOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "updateMany":
                            throw new NotImplementedException();
                        case "updateOne":
                            throw new NotImplementedException();
                        case "watch":
                            throw new NotImplementedException();
                        default:
                            throw new FormatException($"Invalid method name: \"{operationName}\".");
                    }

                case var _ when targetEntityId.StartsWith("bucket"):
                    switch (operationName)
                    {
                        case "download":
                            throw new NotImplementedException();
                        case "download_by_name":
                            throw new NotImplementedException();
                        default:
                            throw new FormatException($"Invalid method name: \"{operationName}\".");
                    }

                default:
                    throw new FormatException($"Target entity type not recognized for entity with id: \"{targetEntityId}\".");
            }
        }
    }
}
