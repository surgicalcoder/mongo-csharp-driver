/* Copyright 2016 MongoDB Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Specifications.bson_corpus
{
    public class TestRunner
    {
        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTest(BsonDocument definition, string testType, BsonDocument test)
        {
            ModifyPlatformDependentTests(definition, testType, test);

            switch (testType)
            {
                case "valid": RunValidTest(test); break;
                case "decodeError": RunDecodeErrorTest(test); break;
                case "parseError": RunParseErrorTest(test); break;
                default: throw new Exception($"Invalid test type: {testType}.");
            }
        }

        // private methods
        private void RunValidTest(BsonDocument test)
        {
            var B = BsonUtils.ParseHexString(test["bson"].AsString);

            byte[] cB;
            if (test.Contains("canonical_bson"))
            {
                cB = BsonUtils.ParseHexString(test["canonical_bson"].AsString);
            }
            else
            {
                cB = B;
            }

            EncodeBson(DecodeBson(B)).Should().Equal(cB, "B -> CB");

            if (B != cB)
            {
                EncodeBson(DecodeBson(cB)).Should().Equal(cB, "cB -> cB");
            }

            if (test.Contains("extjson"))
            {
                var E = test["extjson"].AsString.Replace(" ", "");

                string cE;
                if (test.Contains("canonical_extjson"))
                {
                    cE = test["canonical_extjson"].AsString.Replace(" ", "");
                }
                else
                {
                    cE = E;
                }
                cE = UnescapeUnicodeCharacters(cE);

                EncodeExtjson(DecodeBson(B)).Should().Be(cE, "B -> cE");
                EncodeExtjson(DecodeExtjson(E)).Should().Be(cE, "E -> cE");

                if (B != cB)
                {
                    EncodeExtjson(DecodeBson(cB)).Should().Be(cE, "cB -> cE");
                }

                if (E != cE)
                {
                    EncodeExtjson(DecodeExtjson(cE)).Should().Be(cE, "cE -> cE");
                }

                if (!test.GetValue("lossy", false).ToBoolean())
                {
                    EncodeBson(DecodeExtjson(E)).Should().Equal(cB, "E -> cB");

                    if (E != cE)
                    {
                        EncodeBson(DecodeExtjson(cE)).Should().Equal(cB, "cE -> cB");
                    }
                }
            }
        }

        private void RunDecodeErrorTest(BsonDocument test)
        {
            var bson = BsonUtils.ParseHexString(test["bson"].AsString);

            var exception = Record.Exception(() => BsonSerializer.Deserialize<BsonDocument>(bson));
        }

        private void RunParseErrorTest(BsonDocument test)
        {
            var json = test["string"].AsString;

            var exception = Record.Exception(() => BsonDocument.Parse(json));

            exception.Should().BeOfType<BsonSerializationException>();
        }

        private void ModifyPlatformDependentTests(BsonDocument definition, string testType, BsonDocument test)
        {
            var path = definition["path"].AsString;

            if (path.EndsWith("double.json") && testType == "valid" && test["description"] == "+2.0001220703125e10")
            {
                test["extjson"] = test["extjson"].AsString.Replace("2.0001220703125e10", "20001220703.125");
                return;
            }

            if (path.EndsWith("double.json") && testType == "valid" && test["description"] == "-2.0001220703125e10")
            {
                test["extjson"] = test["extjson"].AsString.Replace("-2.0001220703125e10", "-20001220703.125");
                return;
            }

            if (path.EndsWith("double.json") && testType == "valid" && test["description"] == "-0.0")
            {
                test["canonical_bson"] = "10000000016400000000000000000000";
                test["extjson"] = test["extjson"].AsString.Replace("-0.0", "0.0");
                return;
            }
        }

        private BsonDocument DecodeBson(byte[] bytes)
        {
            var readerSettings = new BsonBinaryReaderSettings { FixOldBinarySubTypeOnInput = false, GuidRepresentation = GuidRepresentation.Unspecified };
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BsonBinaryReader(stream, readerSettings))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                return BsonDocumentSerializer.Instance.Deserialize(context);
            }
        }

        private BsonDocument DecodeExtjson(string extjson)
        {
            return BsonDocument.Parse(extjson);
        }

        private byte[] EncodeBson(BsonDocument document)
        {
            var writerSettings = new BsonBinaryWriterSettings { FixOldBinarySubTypeOnOutput = false, GuidRepresentation = GuidRepresentation.Unspecified };
            using (var stream = new MemoryStream())
            using (var writer = new BsonBinaryWriter(stream, writerSettings))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                BsonDocumentSerializer.Instance.Serialize(context, document);
                return stream.ToArray();
            }
        }

        private string EncodeExtjson(BsonDocument document)
        {
            var json = document.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.ExtendedJson, GuidRepresentation = GuidRepresentation.Unspecified });
            return json.Replace(" ", "");
        }

        private string UnescapeUnicodeCharacters(string value)
        {
            var pattern = @"\\u[0-9a-fA-F]{4}";
            var unescaped = Regex.Replace(value, pattern, match =>
            {
                var bytes = BsonUtils.ParseHexString(match.Value.Substring(2, 4));
                var c = (char)(bytes[0] << 8 | bytes[1]);
                return c == 0 ? match.Value : new string(c, 1);
            });
            return unescaped;
        }

        // nested types
        private class TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
#if NETSTANDARD1_5 || NETSTANDARD1_6
                const string prefix = "MongoDB.Bson.Tests.Dotnet.Specifications.bson_corpus.tests.";
#else
                const string prefix = "MongoDB.Bson.Tests.Specifications.bson_corpus.tests.";
#endif
                var executingAssembly = typeof(TestCaseFactory).GetTypeInfo().Assembly;
                var enumerable = executingAssembly
                    .GetManifestResourceNames()
                    .Where(path => path.StartsWith(prefix) && path.EndsWith(".json"))
                    .SelectMany(path =>
                    {
                        var definition = ReadDefinition(path);
                        var testCases = Enumerable.Empty<object[]>();

                        if (definition["bson_type"] != "0x0C")
                        {
                            if (definition.Contains("valid"))
                            {
                                testCases = testCases.Concat(definition["valid"].AsBsonArray.Select(test => new object[] { definition, "valid", test }));
                            }
                            if (definition.Contains("decodeErrors"))
                            {
                                testCases = testCases.Concat(definition["decodeErrors"].AsBsonArray.Select(test => new object[] { definition, "decodeError", test }));
                            }
                            if (definition.Contains("parseErrors"))
                            {
                                testCases = testCases.Concat(definition["parseErrors"].AsBsonArray.Select(test => new object[] { definition, "parseError", test }));
                            }
                        }

                        return testCases;
                    });

                return enumerable.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }


            private static BsonDocument ReadDefinition(string path)
            {
                var executingAssembly = typeof(TestCaseFactory).GetTypeInfo().Assembly;
                using (var definitionStream = executingAssembly.GetManifestResourceStream(path))
                using (var definitionReader = new StreamReader(definitionStream))
                {
                    var definitionString = definitionReader.ReadToEnd();
                    var definition = BsonDocument.Parse(definitionString);
                    definition.InsertAt(0, new BsonElement("path", path));
                    return definition;
                }
            }
        }
    }
}
