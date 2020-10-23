/* Copyright 2010-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
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
        public void Apple_class_should_be_mapped_as_expected()
        {
            var classMap = new BsonClassMap<Apple>();
            classMap.AutoMap();
            classMap.Freeze();

            classMap.BaseClassMap.ClassType.Should().Be(typeof(Fruit));
            classMap.AllMemberMaps.Select(m => m.MemberName).Should().Equal("Seeds");
            classMap.DeclaredMemberMaps.Select(m => m.MemberName).Should().Equal("Seeds");
            classMap.CreatorMaps.Should().HaveCount(1);
            classMap.Discriminator.Should().Be("Apple");
        }

        [Fact]
        public void Serialize_Apple_should_have_expected_effect()
        {
            var apple = new Apple(123);

            var json = apple.ToJson();

            json.Should().Be("{ \"_t\" : [\"Fruit\", \"Apple\"], \"Seeds\" : 123 }");
        }

        [Fact]
        public void Deserialize_Apple_should_return_expected_result()
        {
            var json = "{ \"_t\" : [\"Fruit\", \"Apple\"], \"Seeds\" : 123 }";

            var apple = BsonSerializer.Deserialize<Apple>(json);

            apple.Should().NotBeNull();
        }
    }
}
