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

using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public enum EnumWithUnderlyingTypeByte : byte
    {
        Zero = 0,
        One = 1,
        Max = byte.MaxValue
    }

    public enum EnumWithUnderlyingTypeUInt32 : uint
    {
        Zero = 0,
        One = 1,
        Max = uint.MaxValue
    }

    public class ClassWithEnumWithUnderlyingTypeByte
    {
        public EnumWithUnderlyingTypeByte E { get; set; }
    }

    public class ClassWithEnumWithUnderlyingTypeUInt32
    {
        public EnumWithUnderlyingTypeUInt32 E { get; set; }
    }

    public class EnumSerializerTests
    {
        [Theory]
        [InlineData(EnumWithUnderlyingTypeByte.Zero, "{ $numberInt : '0' }")]
        [InlineData(EnumWithUnderlyingTypeByte.One, "{ $numberInt : '1' }")]
        [InlineData(EnumWithUnderlyingTypeByte.Max, "{ $numberInt : '255' }")]
        public void EnumWithUnderlyingTypeByte_should_roundtrip(
            EnumWithUnderlyingTypeByte value,
            string expectedRepresentation)
        {
            var original = new ClassWithEnumWithUnderlyingTypeByte{ E = value };

            var bson = original.ToBson();
            var serialized = BsonSerializer.Deserialize<BsonDocument>(bson);
            var deserialized = BsonSerializer.Deserialize<ClassWithEnumWithUnderlyingTypeByte>(bson);

            serialized["E"].Should().Be(expectedRepresentation);
            deserialized.E.Should().Be(original.E);
        }

        [Theory]
        [InlineData(EnumWithUnderlyingTypeUInt32.Zero, "{ $numberInt : '0' }")]
        [InlineData(EnumWithUnderlyingTypeUInt32.One, "{ $numberInt : '1' }")]
        [InlineData(EnumWithUnderlyingTypeUInt32.Max, "{ $numberInt : '-1' }")]
        public void EnumWithUnderlyingTypeUInt32_should_roundtrip(
            EnumWithUnderlyingTypeUInt32 value,
            string expectedRepresentation)
        {
            var original = new ClassWithEnumWithUnderlyingTypeUInt32 { E = value };

            var bson = original.ToBson();
            var serialized = BsonSerializer.Deserialize<BsonDocument>(bson);
            var deserialized = BsonSerializer.Deserialize<ClassWithEnumWithUnderlyingTypeUInt32>(bson);

            serialized["E"].Should().Be(expectedRepresentation);
            deserialized.E.Should().Be(original.E);
        }
    }
}
