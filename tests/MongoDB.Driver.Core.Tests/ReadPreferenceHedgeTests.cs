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
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Tests
{
    public class ReadPreferenceHedgeTests
    {
        [Fact]
        public void Enabled_should_return_expected_result()
        {
            var result = ReadPreferenceHedge.Enabled;

            var enabled = result.Should().BeOfType<CustomReadPreferenceHedge>().Subject;
            enabled.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void ServerDefault_should_return_expected_result()
        {
            var result = ReadPreferenceHedge.ServerDefault;

            result.Should().BeOfType<ServerDefaultReadPreferenceHedge>();
        }

        [Theory]
        [InlineData("false", "null", false)]
        [InlineData("false", "false", true)]
        [InlineData("false", "true", false)]
        [InlineData("false", "serverdefault", false)]
        [InlineData("true", "null", false)]
        [InlineData("true", "false", false)]
        [InlineData("true", "true", true)]
        [InlineData("true", "serverdefault", false)]
        [InlineData("serverdefault", "null", false)]
        [InlineData("serverdefault", "false", false)]
        [InlineData("serverdefault", "true", false)]
        [InlineData("serverdefault", "serverdefault", true)]
        public void Equals_should_return_expected_result(string lhsValue, string rhsValue, bool expectedResult)
        {
            var subject = ReadPreferenceHedgeHelper.Create(lhsValue);
            var other = ReadPreferenceHedgeHelper.Create(rhsValue);

            var result1 = subject.Equals(other);
            var result2 = subject.Equals((object)other);

            result1.Should().Be(expectedResult);
            result2.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("false")]
        [InlineData("true")]
        [InlineData("serverdefault")]
        public void Equals_object_should_return_false(string lhsValue)
        {
            var subject = ReadPreferenceHedgeHelper.Create(lhsValue);
            var other = new object();

            var result = subject.Equals(other);

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("false", "{ enabled : false }")]
        [InlineData("true", "{ enabled : true }")]
        [InlineData("serverdefault", "{ }")]
        public void ToBsonDocument_should_return_expected_result(string hedgeValue, string expectedResult)
        {
            var subject = ReadPreferenceHedgeHelper.Create(hedgeValue);

            var result = subject.ToBsonDocument();

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("false", "{ \"enabled\" : false }")]
        [InlineData("true", "{ \"enabled\" : true }")]
        [InlineData("serverdefault", "{ }")]
        public void ToString_should_return_expected_result(string hedgeValue, string expectedResult)
        {
            var subject = ReadPreferenceHedgeHelper.Create(hedgeValue);

            var result = subject.ToString();

            result.Should().Be(expectedResult);
        }
    }

    public class CustomReadPreferenceHedgeTests
    {
        [Theory]
        [ParameterAttributeData]
        public void constructor_should_initialize_instance(
            [Values(false, true)] bool isEnabled)
        {
            var subject = new CustomReadPreferenceHedge(isEnabled);

            subject.IsEnabled.Should().Be(isEnabled);
        }

        [Theory]
        [ParameterAttributeData]
        public void IsEnabled_should_return_expected_result(
            [Values(false, true)] bool isEnabled)
        {
            var subject = new CustomReadPreferenceHedge(isEnabled);

            var result = subject.IsEnabled;

            result.Should().Be(isEnabled);
        }

        [Theory]
        [ParameterAttributeData]
        public void GetHashCode_should_return_expected_result(
            [Values(false, true)] bool isEnabled)
        {
            var subject = new CustomReadPreferenceHedge(isEnabled);

            var result = subject.GetHashCode();

            var expectedResult = isEnabled.GetHashCode();
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, "{ enabled : false }")]
        [InlineData(true, "{ enabled : true }")]
        public void ToBsonDocument_should_return_expected_result(bool isEnabled, string expectedResult)
        {
            var subject = new CustomReadPreferenceHedge(isEnabled);

            var result = subject.ToBsonDocument();

            result.Should().Be(expectedResult);
        }
    }

    public class ServerDefaultReadPreferenceHedgeTests
    {
        [Fact]
        public void GetHashCode_should_return_expected_result()
        {
            var subject = new ServerDefaultReadPreferenceHedge();

            var result = subject.GetHashCode();

            result.Should().Be(0);
        }

        [Fact]
        public void ToBsonDocument_should_return_expected_result()
        {
            var subject = new ServerDefaultReadPreferenceHedge();

            var result = subject.ToBsonDocument();

            result.Should().Be("{ }");
        }
    }

    public static class ReadPreferenceHedgeHelper
    {
        public static ReadPreferenceHedge Create(string value)
        {
            switch (value)
            {
                case "null": return null;
                case "serverdefault": return new ServerDefaultReadPreferenceHedge();
                case "false": return new CustomReadPreferenceHedge(isEnabled: false);
                case "true": return new CustomReadPreferenceHedge(isEnabled: true);
                default: throw new Exception($"Unexpected value: \"{value}\".");
            }
        }
    }
}
