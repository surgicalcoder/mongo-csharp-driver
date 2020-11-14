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
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.unified_test_format
{
    public class UnifiedValueMatcher
    {
        private EntityMap _entityMap;

        public UnifiedValueMatcher(EntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public void AssertValuesMatch(BsonValue expected, BsonValue actual)
        {
            AssertValuesMatch(expected, actual, isRoot: true);
        }

        private void AssertValuesMatch(BsonValue expected, BsonValue actual, bool isRoot)
        {
            if (expected.IsBsonDocument &&
                expected.AsBsonDocument.ElementCount == 1 &&
                expected.AsBsonDocument.GetElement(0).Name.StartsWith("$$"))
            {
                var expectedDocument = expected.AsBsonDocument; // TODO: Recheck if non document allowed
                switch (expectedDocument.GetElement(0).Name)
                {
                    case "$$unsetOrMatches":
                        // TODO: recheck
                        if (actual == null)
                        {
                            return;
                        }
                        expected = expectedDocument[0];
                        break;
                    case "$$type":
                        AssertExpectedType(actual, expectedDocument[0]);
                        return;
                    case "$$matchesHexBytes":
                        // TODO: recheck
                        expected = expectedDocument[0];
                        break;
                    default:
                        throw new NotSupportedException($"Special operator not supported: '{expectedDocument.GetElement(0).Name}'");
                }
            }

            if (expected.IsBsonDocument)
            {
                actual.BsonType.Should().Be(BsonType.Document);

                var expectedDocument = expected.AsBsonDocument;
                var actualDocument = actual.AsBsonDocument;

                foreach (var expectedElement in expectedDocument)
                {
                    BsonValue expectedItem = expectedElement.Value;
                    // TODO: consider using var t = expectedElement.Value is BsonDocument expectedDocument1;
                    if (expectedElement.Value.IsBsonDocument &&
                        expectedElement.Value.AsBsonDocument.ElementCount == 1 &&
                        expectedElement.Value.AsBsonDocument.GetElement(0).Name.StartsWith("$$"))
                    {
                        var specialOperator = expectedElement.Value.AsBsonDocument;
                        switch (specialOperator.GetElement(0).Name) // TODO: Recheck the switch order
                        {
                            case "$$exists":
                                actualDocument.Contains(expectedElement.Name).Should().Be(specialOperator[0].AsBoolean); // TODO: Recheck this actually works
                                continue;
                            case "$$type":
                                AssertExpectedType(actual, specialOperator[0]);
                                continue;
                            case "$$unsetOrMatches":
                                if (!actualDocument.Contains(expectedElement.Name))
                                {
                                    return;
                                }
                                expectedItem = specialOperator[0];
                                break;
                            case "$$matchesEntity":
                                var resultId = specialOperator[0].AsString;
                                expectedItem = _entityMap.GetResult(resultId);
                                break;
                            case "$$matchesHexBytes":
                                // TODO: recheck
                                expectedItem = specialOperator[0];
                                break;
                            default:
                                throw new NotSupportedException($"Special operator not supported: '{specialOperator.GetElement(0).Name}'");
                        }
                    }

                    actualDocument.Contains(expectedElement.Name).Should().BeTrue($"Actual document must contain key: {expectedElement.Name}");
                    AssertValuesMatch(expectedItem, actualDocument[expectedElement.Name], isRoot: false);
                }

                if (!isRoot)
                {
                    foreach (var name in actualDocument.Names)
                    {
                        expectedDocument.Contains(name).Should().BeTrue($"Actual document contains unexpected key: {name}");
                    }
                }
            }
            else if (expected.IsBsonArray)
            {
                actual.IsBsonArray.Should().BeTrue($"Actual value must be a document, but is '{actual.BsonType}'");
                actual.AsBsonArray.Count.Should().Be(expected.AsBsonArray.Count, "Arrays must the be same size"); // TODO: Check if sizes are included in error message

                var expectedArray = expected.AsBsonArray;
                var actualArray = actual.AsBsonArray;

                for (int i = 0; i < expectedArray.Count; i++)
                {
                    AssertValuesMatch(expectedArray[i], actualArray[i], isRoot: false);
                }
            }
            else if (expected.IsNumeric)
            {
                actual.IsNumeric.Should().BeTrue($"Actual value must be numeric, but is '{actual.BsonType}'");

                actual.ToDouble().Should().Be(expected.ToDouble()); // TODO: Cast them to double? What about epsilon? Or maybe check each number type individually?
            }
            else
            {
                actual.BsonType.Should().Be(expected.BsonType);
                actual.Should().Be(expected);
            }
        }

        private void AssertExpectedType(BsonValue actual, BsonValue expectedTypes)
        {
            var actualTypeName = GetBsonTypeNameAsString(actual.BsonType);
            List<string> expectedTypeNames;

            if (expectedTypes.IsString)
            {
                expectedTypeNames = new List<string> { expectedTypes.AsString };
            }
            else if (expectedTypes.IsBsonArray)
            {
                expectedTypeNames = expectedTypes.AsBsonArray.Select(t => t.AsString).ToList();
            }
            else
            {
                throw new FormatException($"Unexpected $$type value BsonType: '{expectedTypes.BsonType}'");
            }

            actualTypeName.Should().BeOneOf(expectedTypeNames);
        }

        private string GetBsonTypeNameAsString(BsonType bsonType)
        {
            switch (bsonType)
            {
                case BsonType.Double:
                    return "double";
                case BsonType.String:
                    return "string";
                case BsonType.Document:
                    return "document";
                case BsonType.Array:
                    return "array";
                case BsonType.Binary:
                    return "binData";
                case BsonType.ObjectId:
                    return "objectId";
                case BsonType.Boolean:
                    return "bool";
                case BsonType.DateTime:
                    return "date";
                case BsonType.Null:
                    return "null";
                case BsonType.RegularExpression:
                    return "regex";
                case BsonType.Int32:
                    return "int";
                case BsonType.Timestamp:
                    return "timestamp";
                case BsonType.Int64:
                    return "long";
                case BsonType.Decimal128:
                    return "decimal";
                default:
                    throw new NotSupportedException($"Bson type string conversion not supported: '{bsonType}'");
            }
        }
    }
}
