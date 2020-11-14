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
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.unified_test_format
{
    public class UnifiedErrorMatcher
    {
        public void AssertErrorsMatch(BsonDocument expectedError, Exception actualException)
        {
            expectedError.Elements.Should().NotBeEmpty();

            foreach (var element in expectedError)
            {
                switch (element.Name)
                {
                    case "isError":
                        var isError = element.Value.AsBoolean;
                        isError.Should().BeTrue("Test files MUST NOT specify false.");
                        actualException.Should().NotBeNull();
                        break;
                    case "isClientError":
                        var isClientError = element.Value.AsBoolean;
                        // TODO: Recheck assertion and add types as necessary.
                        var actualIsClientError =
                            actualException is MongoClientException ||
                            actualException is BsonException;
                        actualIsClientError.Should().Be(isClientError);
                        break;
                    case "errorContains":
                        var expectedSubstring = element.Value.AsString;
                        actualException.Message.Should().ContainEquivalentOf(expectedSubstring);
                        break;
                    case "errorCode":
                        var errorCode = element.Value.AsInt32;
                        // TODO: Add exception type assertion.
                        // TODO: Check in debug.
                        {
                            var mongoCommandException = actualException as MongoCommandException;
                            mongoCommandException.Code.Should().Be(errorCode);
                        }
                        break;
                    case "errorCodeName":
                        var errorCodeName = element.Value.AsString;
                        // TODO: Add exception type assertion.
                        // TODO: Check in debug.
                        {
                            var mongoCommandException = actualException as MongoCommandException;
                            mongoCommandException.CodeName.Should().Be(errorCodeName);
                        }
                        break;
                    case "errorLabelsContain":
                        var expectedErrorLabels = element.Value.AsBsonArray.Select(x => x.AsString);
                        // TODO: Add exception type assertion.
                        // TODO: Check in debug.
                        {
                            var mongoCommandException = actualException as MongoException;
                            mongoCommandException.ErrorLabels.Should().Contain(expectedErrorLabels);
                        }
                        break;
                    case "errorLabelsOmit":
                        var expectedAbsentErrorLabels = element.Value.AsBsonArray.Select(x => x.AsString);
                        // TODO: Add exception type assertion.
                        // TODO: Check in debug.
                        {
                            var mongoCommandException = actualException as MongoException;
                            mongoCommandException.ErrorLabels.Should().NotContain(expectedAbsentErrorLabels);
                        }
                        break;
                    case "expectResult":
                        var expectResult = element.Value.AsBsonDocument;
                        // TODO: Implement case.
                        break;
                    default:
                        throw new FormatException($"Unrecognized error assertion: '{element.Name}'");
                }
            }
        }
    }
}
