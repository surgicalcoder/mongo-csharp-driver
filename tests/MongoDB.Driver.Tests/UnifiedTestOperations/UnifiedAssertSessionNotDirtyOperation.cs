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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Tests.Specifications.unified_test_format;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedAssertSessionNotDirtyOperation : IUnifiedSpecialTestOperation
    {
        private readonly IClientSessionHandle _session;

        public UnifiedAssertSessionNotDirtyOperation(IClientSessionHandle session)
        {
            _session = session;
        }

        public void Execute()
        {
            _session.WrappedCoreSession.IsDirty.Should().BeFalse();
        }
    }

    public class UnifiedAssertSessionNotDirtyOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedAssertSessionNotDirtyOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;

        }

        public UnifiedAssertSessionNotDirtyOperation Build(BsonDocument arguments)
        {
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "session":
                        session = _entityMap.GetSession(argument.Value.AsString);
                        break;
                    default:
                        throw new FormatException($"Invalid AssertSessionNotDirtyOperation argument name: '{argument.Name}'");
                }
            }

            return new UnifiedAssertSessionNotDirtyOperation(session);
        }
    }
}
