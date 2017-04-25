/* Copyright 2017 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.IO.JsonConverters;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class JsonOutputConvertersTests
    {
        [Fact]
        public void Shell_get_should_return_expected_result()
        {
            var result = JsonOutputConverters.Shell;

            result.BinaryDataConverter.Should().BeOfType<BsonBinaryDataShellJsonConverter>();
            result.BooleanConverter.Should().BeOfType<BooleanStrictJsonConverter>();
            result.DateTimeConverter.Should().BeOfType<BsonDateTimeShellJsonConverter>();
            result.Decimal128Converter.Should().BeOfType<Decimal128ShellJsonConverter>();
            result.DoubleConverter.Should().BeOfType<DoubleWithDecimalPointJsonConverter>();
            result.Int32Converter.Should().BeOfType<Int32StrictJsonConverter>();
            result.Int64Converter.Should().BeOfType<Int64ShellJsonConverter>();
            result.JavaScriptConverter.Should().BeOfType<BsonJavaScriptExtendedJsonConverter>();
            result.MaxKeyConverter.Should().BeOfType<BsonMaxKeyShellJsonConverter>();
            result.MinKeyConverter.Should().BeOfType<BsonMinKeyShellJsonConverter>();
            result.NullConverter.Should().BeOfType<BsonNullStrictJsonConverter>();
            result.ObjectIdConverter.Should().BeOfType<ObjectIdShellJsonConverter>();
            result.RegularExpressionConverter.Should().BeOfType<BsonRegularExpressionShellJsonConverter>();
            result.StringConverter.Should().BeOfType<StringStrictJsonConverter>();
            result.SymbolConverter.Should().BeOfType<BsonSymbolExtendedJsonConverter>();
            result.TimestampConverter.Should().BeOfType<BsonTimestampShellJsonConverter>();
            result.UndefinedConverter.Should().BeOfType<BsonUndefinedShellJsonConverter>();
        }

        [Fact]
        public void Shell_get_should_return_same_instance()
        {
            var result1 = JsonOutputConverters.Shell;
            var result2 = JsonOutputConverters.Shell;

            result2.Should().BeSameAs(result1);
        }

        [Fact]
        public void Strict_get_should_return_expected_result()
        {
            var result = JsonOutputConverters.Strict;

            result.BinaryDataConverter.Should().BeOfType<BsonBinaryDataExtendedJsonConverter>();
            result.BooleanConverter.Should().BeOfType<BooleanStrictJsonConverter>();
            result.DateTimeConverter.Should().BeOfType<BsonDateTimeExtendedJsonConverter>();
            result.Decimal128Converter.Should().BeOfType<Decimal128ExtendedJsonConverter>();
            result.DoubleConverter.Should().BeOfType<DoubleWithDecimalPointJsonConverter>();
            result.Int32Converter.Should().BeOfType<Int32StrictJsonConverter>();
            result.Int64Converter.Should().BeOfType<Int64StrictJsonConverter>();
            result.JavaScriptConverter.Should().BeOfType<BsonJavaScriptExtendedJsonConverter>();
            result.MaxKeyConverter.Should().BeOfType<BsonMaxKeyExtendedJsonConverter>();
            result.MinKeyConverter.Should().BeOfType<BsonMinKeyExtendedJsonConverter>();
            result.NullConverter.Should().BeOfType<BsonNullStrictJsonConverter>();
            result.ObjectIdConverter.Should().BeOfType<ObjectIdExtendedJsonConverter>();
            result.RegularExpressionConverter.Should().BeOfType<BsonRegularExpressionExtendedJsonConverter>();
            result.StringConverter.Should().BeOfType<StringStrictJsonConverter>();
            result.SymbolConverter.Should().BeOfType<BsonSymbolExtendedJsonConverter>();
            result.TimestampConverter.Should().BeOfType<BsonTimestampExtendedJsonConverter>();
            result.UndefinedConverter.Should().BeOfType<BsonUndefinedExtendedJsonConverter>();
        }

        [Fact]
        public void Strict_get_should_return_same_instance()
        {
            var result1 = JsonOutputConverters.Strict;
            var result2 = JsonOutputConverters.Strict;

            result2.Should().BeSameAs(result1);
        }
    }
}
