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

using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    // Note: including these tests as part of the work on CSHARP-3240 to verify that this also fixes CSHARP-2984

    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(Apple))]
    [BsonIgnoreExtraElements]
    public abstract class Fruit
    {
        [BsonIgnore]
        public abstract string Color { get; }
    }

    [BsonIgnoreExtraElements]
    public class Apple : Fruit
    {
        [BsonConstructor]
        public Apple(int seeds)
        {
            Seeds = seeds;
        }

        public int Seeds { get; }

        [BsonIgnore]
        public override string Color => "Red";
    }

    public class CSharp2984Tests
    {
        [Fact]
        public void Serialize_should_return_have_expected_result()
        {
            var subject = new Apple(1);

            var result = subject.ToJson();

            result.Should().Be("{ \"_t\" : [\"Fruit\", \"Apple\"], \"Seeds\" : 1 }");
        }

        [Fact]
        public void Deserialize_should_return_expected_result()
        {
            var json = "{ \"_t\" : [\"Fruit\", \"Apple\"], \"Seeds\" : 1 }";

            var result = BsonSerializer.Deserialize<Fruit>(json);

            var apple = result.Should().BeOfType<Apple>().Subject;
            apple.Seeds.Should().Be(1);
            apple.Color.Should().Be("Red");
        }
    }
}
