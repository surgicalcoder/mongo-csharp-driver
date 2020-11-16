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

namespace MongoDB.Driver.Tests.Specifications.unified_test_format.UnifiedTestOperations
{
    public class UnifiedListDatabasesOperation : IUnifiedTestOperation
    {
        private readonly IMongoClient _client;

        public UnifiedListDatabasesOperation(IMongoClient client)
        {
            _client = client;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var cursor = _client.ListDatabases(cancellationToken);
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
                var cursor = await _client.ListDatabasesAsync(cancellationToken);
                var result = await cursor.ToListAsync();
                return new OperationResult(new BsonArray(result));
            }
            catch (Exception exception)
            {
                return new OperationResult(exception);
            }
        }
    }
}
