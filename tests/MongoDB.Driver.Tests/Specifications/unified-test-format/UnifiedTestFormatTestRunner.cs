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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Tests.Specifications.unified_test_format.UnifiedTestOperations;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.unified_test_format
{
    public sealed class UnifiedTestFormatTestRunner : IDisposable
    {
        private EntityMap _entityMap;
        private List<FailPoint> _failPoints = new List<FailPoint>();

        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void Run(JsonDrivenTestCase testCase)
        {
            Run(schemaVersion: testCase.Shared["schemaVersion"].AsString,
                runOnRequirements: testCase.Shared.GetValue("runOnRequirements", null)?.AsBsonArray,
                entities: testCase.Shared["createEntities"].AsBsonArray, // TODO: can be absent
                initialData: testCase.Shared.GetValue("initialData", null)?.AsBsonArray,
                test: testCase.Test);
        }

        public void Run(
            string schemaVersion,
            BsonArray runOnRequirements,
            BsonArray entities,
            BsonArray initialData,
            BsonDocument test)
        {
            schemaVersion.Should().StartWith("1.0");
            if (runOnRequirements != null)
            {
                RequireServer.Check().RunOn(runOnRequirements);
            }

            _entityMap = new EntityMap(entities);

            if (initialData != null)
            {
                AddInitialData(DriverTestConfiguration.Client, initialData);
            }

            foreach (var operationItem in test["operations"].AsBsonArray)
            {
                var operation = CreateOperation(operationItem.AsBsonDocument, _entityMap);
                var result = ExecuteOperation(operation, test["async"].AsBoolean);
                if (result != null)
                {
                    AssertResult(result, operationItem.AsBsonDocument, _entityMap);
                }
                if (operation is UnifiedFailPointOperation failPointOperation)
                {
                    _failPoints.Add(failPointOperation.FailPoint);
                }
            }

            if (test.AsBsonDocument.TryGetValue("outcome", out var expectedOutcome))
            {
                AssertOutcome(DriverTestConfiguration.Client, expectedOutcome.AsBsonArray);
            }
            if (test.AsBsonDocument.TryGetValue("expectEvents", out var expectedEvents))
            {
                AssertEvents(expectedEvents.AsBsonArray, _entityMap);
            }
        }

        public void Dispose()
        {
            foreach (var failPoint in _failPoints)
            {
                failPoint?.Dispose();
            }
            _entityMap?.Dispose();
        }

        // private methods
        private void AddInitialData(IMongoClient client, BsonArray initialData)
        {
            foreach (var dataItem in initialData)
            {
                var collectionName = dataItem["collectionName"].AsString;
                var databaseName = dataItem["databaseName"].AsString;
                var documents = dataItem["documents"].AsBsonArray.Cast<BsonDocument>().ToList();

                var database = client.GetDatabase(databaseName);
                var collection = database
                    .GetCollection<BsonDocument>(collectionName)
                    .WithWriteConcern(WriteConcern.WMajority);

                database.DropCollection(collectionName);
                if (documents.Count > 0)
                {
                    collection.InsertMany(documents);
                }
                else
                {
                    database.CreateCollection(collectionName); // TODO: Use "majority" write concern.
                }
            }
        }

        // alphabetical order?
        private void AssertResult(OperationResult actualResult, BsonDocument operation, EntityMap entityMap)
        {
            if (operation.TryGetValue("expectResult", out var expectedResult))
            {
                actualResult.Exception.Should().BeNull();
                new UnifiedValueMatcher(entityMap).AssertValuesMatch(actualResult.Result, expectedResult);
            }
            if (operation.TryGetValue("expectError", out var expectedError))
            {
                actualResult.Result.Should().BeNull();
                actualResult.Exception.Should().NotBeNull();
                new UnifiedErrorMatcher().AssertErrorsMatch(actualResult.Exception, expectedError.AsBsonDocument);
            }
            else
            {
                actualResult.Exception.Should().BeNull();
            }
        }

        private void AssertOutcome(IMongoClient client, BsonArray outcome)
        {
            foreach (var outcomeItem in outcome)
            {
                var collectionName = outcomeItem["collectionName"].AsString;
                var databaseName = outcomeItem["databaseName"].AsString;
                var expectedData = outcomeItem["documents"].AsBsonArray.Cast<BsonDocument>().ToList();

                var actualData = client
                    .GetDatabase(databaseName)
                    .GetCollection<BsonDocument>(collectionName)
                    .Find(new EmptyFilterDefinition<BsonDocument>())
                    .ToList();

                // TODO: Recheck spec requirements regarding data order
                actualData.Should().BeEquivalentTo(expectedData);
            }
        }

        private void AssertEvents(BsonArray eventItems, EntityMap entityMap)
        {
            var unifiedEventMatcher = new UnifiedEventMatcher(new UnifiedValueMatcher(entityMap));
            foreach (var eventItem in eventItems)
            {
                var clientId = eventItem["client"].AsString;
                var eventCapturer = entityMap.GetEventCapturer(clientId);
                var actualEvents = eventCapturer.Events;

                unifiedEventMatcher.AssertEventsMatch(actualEvents, eventItem["events"].AsBsonArray);
            }
        }

        private IUnifiedTestOperation CreateOperation(BsonDocument operation, EntityMap entityMap)
        {
            var factory = new UnifiedTestOperationFactory(entityMap);

            var operationName = operation["name"].AsString;
            var operationTarget = operation["object"].AsString;
            var operationArguments = operation.GetValue("arguments", null)?.AsBsonDocument;

            return factory.CreateOperation(operationName, operationTarget, operationArguments);
        }

        private OperationResult ExecuteOperation(IUnifiedTestOperation operation, bool async)
        {
            if (async)
            {
                return operation.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                return operation.Execute(CancellationToken.None);
            }
        }

        // nested types
        public class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            // protected properties
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.unified_test_format.tests.valid_pass";

            // protected methods
            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                foreach (var testCase in base.CreateTestCases(document))
                {
                    foreach (var async in new[] { false, true })
                    {
                        var name = $"{testCase.Name}:async={async}";
                        var test = testCase.Test.DeepClone().AsBsonDocument.Add("async", async);
                        yield return new JsonDrivenTestCase(name, testCase.Shared, test);
                    }
                }
            }
        }
    }
}
