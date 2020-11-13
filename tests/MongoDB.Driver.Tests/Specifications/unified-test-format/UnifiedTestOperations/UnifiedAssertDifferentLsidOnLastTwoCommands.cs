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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Tests.Specifications.unified_test_format.UnifiedTestOperations
{
    public class UnifiedAssertDifferentLsidOnLastTwoCommands : IUnifiedTestOperation
    {
        private readonly EventCapturer _eventCapturer;

        public UnifiedAssertDifferentLsidOnLastTwoCommands(EventCapturer eventCapturer)
        {
            _eventCapturer = eventCapturer;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            var lastTwoCommands = _eventCapturer
                .Events
                .Skip(_eventCapturer.Events.Count - 2)
                .Select(commandStartedEvent => ((CommandStartedEvent)commandStartedEvent).Command)
                .ToList();

            AssertDifferentLsid(lastTwoCommands[0], lastTwoCommands[1]);

            return null;
        }

        public Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute(cancellationToken));
        }

        // private methods
        private void AssertDifferentLsid(BsonDocument first, BsonDocument second)
        {
            first["lsid"].Should().NotBe(second["lsid"]);
        }
    }
}
