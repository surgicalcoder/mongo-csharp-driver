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
    public class JsonConverterSetTests
    {
        [Fact]
        public void Shell_get_should_return_expected_result()
        {
            var result = JsonConverterSet.Shell;

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
            var result1 = JsonConverterSet.Shell;
            var result2 = JsonConverterSet.Shell;

            result2.Should().BeSameAs(result1);
        }

        [Fact]
        public void Strict_get_should_return_expected_result()
        {
            var result = JsonConverterSet.Strict;

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
            var result1 = JsonConverterSet.Strict;
            var result2 = JsonConverterSet.Strict;

            result2.Should().BeSameAs(result1);
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var binaryDataConverter = new Mock<IJsonConverter<BsonBinaryData>>().Object;
            var booleanConverter = new Mock<IJsonConverter<bool>>().Object;
            var dateTimeConverter = new Mock<IJsonConverter<long>>().Object;
            var decimal128Converter = new Mock<IJsonConverter<Decimal128>>().Object;
            var doubleConverter = new Mock<IJsonConverter<double>>().Object;
            var int32Converter = new Mock<IJsonConverter<int>>().Object;
            var int64Converter = new Mock<IJsonConverter<long>>().Object;
            var javaScriptConverter = new Mock<IJsonConverter<string>>().Object;
            var maxKeyConverter = new Mock<IJsonConverter<BsonMaxKey>>().Object;
            var minKeyConverter = new Mock<IJsonConverter<BsonMinKey>>().Object;
            var nullConverter = new Mock<IJsonConverter<BsonNull>>().Object;
            var objectIdConverter = new Mock<IJsonConverter<ObjectId>>().Object;
            var regularExpressionConverter = new Mock<IJsonConverter<BsonRegularExpression>>().Object;
            var stringConverter = new Mock<IJsonConverter<string>>().Object;
            var symbolConverter = new Mock<IJsonConverter<string>>().Object;
            var timestampConverter = new Mock<IJsonConverter<long>>().Object;
            var undefinedConverter = new Mock<IJsonConverter<BsonUndefined>>().Object;

            var result = new JsonConverterSet(
                binaryDataConverter,
                booleanConverter,
                dateTimeConverter,
                decimal128Converter,
                doubleConverter,
                int32Converter,
                int64Converter,
                javaScriptConverter,
                maxKeyConverter,
                minKeyConverter,
                nullConverter,
                objectIdConverter,
                regularExpressionConverter,
                stringConverter,
                symbolConverter,
                timestampConverter,
                undefinedConverter);

            result.BinaryDataConverter.Should().BeSameAs(binaryDataConverter);
            result.BooleanConverter.Should().BeSameAs(booleanConverter);
            result.DateTimeConverter.Should().BeSameAs(dateTimeConverter);
            result.Decimal128Converter.Should().BeSameAs(decimal128Converter);
            result.DoubleConverter.Should().BeSameAs(doubleConverter);
            result.Int32Converter.Should().BeSameAs(int32Converter);
            result.Int64Converter.Should().BeSameAs(int64Converter);
            result.JavaScriptConverter.Should().BeSameAs(javaScriptConverter);
            result.MaxKeyConverter.Should().BeSameAs(maxKeyConverter);
            result.MinKeyConverter.Should().BeSameAs(minKeyConverter);
            result.NullConverter.Should().BeSameAs(nullConverter);
            result.ObjectIdConverter.Should().BeSameAs(objectIdConverter);
            result.RegularExpressionConverter.Should().BeSameAs(regularExpressionConverter);
            result.StringConverter.Should().BeSameAs(stringConverter);
            result.SymbolConverter.Should().BeSameAs(symbolConverter);
            result.TimestampConverter.Should().BeSameAs(timestampConverter);
            result.UndefinedConverter.Should().BeSameAs(undefinedConverter);
        }

        [Theory]
        [InlineData("binaryDataConverter")]
        [InlineData("booleanConverter")]
        [InlineData("dateTimeConverter")]
        [InlineData("decimal128Converter")]
        [InlineData("doubleConverter")]
        [InlineData("int32Converter")]
        [InlineData("int64Converter")]
        [InlineData("javaScriptConverter")]
        [InlineData("maxKeyConverter")]
        [InlineData("minKeyConverter")]
        [InlineData("nullConverter")]
        [InlineData("objectIdConverter")]
        [InlineData("regularExpressionConverter")]
        [InlineData("stringConverter")]
        [InlineData("symbolConverter")]
        [InlineData("timestampConverter")]
        [InlineData("undefinedConverter")]
        public void constructor_should_throw_when_parameter_is_null(string nullParameterName)
        {
            var binaryDataConverter = nullParameterName == "binaryDataConverter" ? null : new Mock<IJsonConverter<BsonBinaryData>>().Object;
            var booleanConverter = nullParameterName == "booleanConverter" ? null : new Mock<IJsonConverter<bool>>().Object;
            var dateTimeConverter = nullParameterName == "dateTimeConverter" ? null : new Mock<IJsonConverter<long>>().Object;
            var decimal128Converter = nullParameterName == "decimal128Converter" ? null : new Mock<IJsonConverter<Decimal128>>().Object;
            var doubleConverter = nullParameterName == "doubleConverter" ? null : new Mock<IJsonConverter<double>>().Object;
            var int32Converter = nullParameterName == "int32Converter" ? null : new Mock<IJsonConverter<int>>().Object;
            var int64Converter = nullParameterName == "int64Converter" ? null : new Mock<IJsonConverter<long>>().Object;
            var javaScriptConverter = nullParameterName == "javaScriptConverter" ? null : new Mock<IJsonConverter<string>>().Object;
            var maxKeyConverter = nullParameterName == "maxKeyConverter" ? null : new Mock<IJsonConverter<BsonMaxKey>>().Object;
            var minKeyConverter = nullParameterName == "minKeyConverter" ? null : new Mock<IJsonConverter<BsonMinKey>>().Object;
            var nullConverter = nullParameterName == "nullConverter" ? null : new Mock<IJsonConverter<BsonNull>>().Object;
            var objectIdConverter = nullParameterName == "objectIdConverter" ? null : new Mock<IJsonConverter<ObjectId>>().Object;
            var regularExpressionConverter = nullParameterName == "regularExpressionConverter" ? null : new Mock<IJsonConverter<BsonRegularExpression>>().Object;
            var stringConverter = nullParameterName == "stringConverter" ? null : new Mock<IJsonConverter<string>>().Object;
            var symbolConverter = nullParameterName == "symbolConverter" ? null : new Mock<IJsonConverter<string>>().Object;
            var timestampConverter = nullParameterName == "timestampConverter" ? null : new Mock<IJsonConverter<long>>().Object;
            var undefinedConverter = nullParameterName == "undefinedConverter" ? null : new Mock<IJsonConverter<BsonUndefined>>().Object;

            var exception = Record.Exception(() => new JsonConverterSet(
                binaryDataConverter,
                booleanConverter,
                dateTimeConverter,
                decimal128Converter,
                doubleConverter,
                int32Converter,
                int64Converter,
                javaScriptConverter,
                maxKeyConverter,
                minKeyConverter,
                nullConverter,
                objectIdConverter,
                regularExpressionConverter,
                stringConverter,
                symbolConverter,
                timestampConverter,
                undefinedConverter));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be(nullParameterName);
        }

        [Fact]
        public void With_binaryDataConverter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var binaryDataConverter = new Mock<IJsonConverter<BsonBinaryData>>().Object;

            var result = subject.With(binaryDataConverter: binaryDataConverter);

            result.BinaryDataConverter.Should().BeSameAs(binaryDataConverter);
            FindDifferences(subject, result).Should().Equal("BinaryDataConverter");
        }

        [Fact]
        public void With_booleanConverter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var booleanConverter = new Mock<IJsonConverter<bool>>().Object;

            var result = subject.With(booleanConverter: booleanConverter);

            result.BooleanConverter.Should().BeSameAs(booleanConverter);
            FindDifferences(subject, result).Should().Equal("BooleanConverter");
        }

        [Fact]
        public void With_dateTimeConverter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var dateTimeConverter = new Mock<IJsonConverter<long>>().Object;

            var result = subject.With(dateTimeConverter: dateTimeConverter);

            result.DateTimeConverter.Should().BeSameAs(dateTimeConverter);
            FindDifferences(subject, result).Should().Equal("DateTimeConverter");
        }

        [Fact]
        public void With_decimal128Converter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var decimal128Converter = new Mock<IJsonConverter<Decimal128>>().Object;

            var result = subject.With(decimal128Converter: decimal128Converter);

            result.Decimal128Converter.Should().BeSameAs(decimal128Converter);
            FindDifferences(subject, result).Should().Equal("Decimal128Converter");
        }

        [Fact]
        public void With_doubleConverter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var doubleConverter = new Mock<IJsonConverter<double>>().Object;

            var result = subject.With(doubleConverter: doubleConverter);

            result.DoubleConverter.Should().BeSameAs(doubleConverter);
            FindDifferences(subject, result).Should().Equal("DoubleConverter");
        }

        [Fact]
        public void With_int32Converter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var int32Converter = new Mock<IJsonConverter<int>>().Object;

            var result = subject.With(int32Converter: int32Converter);

            result.Int32Converter.Should().BeSameAs(int32Converter);
            FindDifferences(subject, result).Should().Equal("Int32Converter");
        }

        [Fact]
        public void With_int64Converter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var int64Converter = new Mock<IJsonConverter<long>>().Object;

            var result = subject.With(int64Converter: int64Converter);

            result.Int64Converter.Should().BeSameAs(int64Converter);
            FindDifferences(subject, result).Should().Equal("Int64Converter");
        }

        [Fact]
        public void With_javaScriptConverter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var javaScriptConverter = new Mock<IJsonConverter<string>>().Object;

            var result = subject.With(javaScriptConverter: javaScriptConverter);

            result.JavaScriptConverter.Should().BeSameAs(javaScriptConverter);
            FindDifferences(subject, result).Should().Equal("JavaScriptConverter");
        }

        [Fact]
        public void With_maxKeyConverter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var maxKeyConverter = new Mock<IJsonConverter<BsonMaxKey>>().Object;

            var result = subject.With(maxKeyConverter: maxKeyConverter);

            result.MaxKeyConverter.Should().BeSameAs(maxKeyConverter);
            FindDifferences(subject, result).Should().Equal("MaxKeyConverter");
        }

        [Fact]
        public void With_minKeyConverter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var minKeyConverter = new Mock<IJsonConverter<BsonMinKey>>().Object;

            var result = subject.With(minKeyConverter: minKeyConverter);

            result.MinKeyConverter.Should().BeSameAs(minKeyConverter);
            FindDifferences(subject, result).Should().Equal("MinKeyConverter");
        }

        [Fact]
        public void With_nullConverter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var nullConverter = new Mock<IJsonConverter<BsonNull>>().Object;

            var result = subject.With(nullConverter: nullConverter);

            result.NullConverter.Should().BeSameAs(nullConverter);
            FindDifferences(subject, result).Should().Equal("NullConverter");
        }

        [Fact]
        public void With_objectIdConverter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var objectIdConverter = new Mock<IJsonConverter<ObjectId>>().Object;

            var result = subject.With(objectIdConverter: objectIdConverter);

            result.ObjectIdConverter.Should().BeSameAs(objectIdConverter);
            FindDifferences(subject, result).Should().Equal("ObjectIdConverter");
        }

        [Fact]
        public void With_regularExpressionConverter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var regularExpressionConverter = new Mock<IJsonConverter<BsonRegularExpression>>().Object;

            var result = subject.With(regularExpressionConverter: regularExpressionConverter);

            result.RegularExpressionConverter.Should().BeSameAs(regularExpressionConverter);
            FindDifferences(subject, result).Should().Equal("RegularExpressionConverter");
        }

        [Fact]
        public void With_stringConverter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var stringConverter = new Mock<IJsonConverter<string>>().Object;

            var result = subject.With(stringConverter: stringConverter);

            result.StringConverter.Should().BeSameAs(stringConverter);
            FindDifferences(subject, result).Should().Equal("StringConverter");
        }

        [Fact]
        public void With_symbolConverter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var symbolConverter = new Mock<IJsonConverter<string>>().Object;

            var result = subject.With(symbolConverter: symbolConverter);

            result.SymbolConverter.Should().BeSameAs(symbolConverter);
            FindDifferences(subject, result).Should().Equal("SymbolConverter");
        }

        [Fact]
        public void With_timestampConverter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var timestampConverter = new Mock<IJsonConverter<long>>().Object;

            var result = subject.With(timestampConverter: timestampConverter);

            result.TimestampConverter.Should().BeSameAs(timestampConverter);
            FindDifferences(subject, result).Should().Equal("TimestampConverter");
        }

        [Fact]
        public void With_undefinedConverter_should_return_expected_result()
        {
            var subject = CreateSubject();
            var undefinedConverter = new Mock<IJsonConverter<BsonUndefined>>().Object;

            var result = subject.With(undefinedConverter: undefinedConverter);

            result.UndefinedConverter.Should().BeSameAs(undefinedConverter);
            FindDifferences(subject, result).Should().Equal("UndefinedConverter");
        }

        // private methods
        private JsonConverterSet CreateSubject()
        {
            return new JsonConverterSet(
                new Mock<IJsonConverter<BsonBinaryData>>().Object,
                new Mock<IJsonConverter<bool>>().Object,
                new Mock<IJsonConverter<long>>().Object,
                new Mock<IJsonConverter<Decimal128>>().Object,
                new Mock<IJsonConverter<double>>().Object,
                new Mock<IJsonConverter<int>>().Object,
                new Mock<IJsonConverter<long>>().Object,
                new Mock<IJsonConverter<string>>().Object,
                new Mock<IJsonConverter<BsonMaxKey>>().Object,
                new Mock<IJsonConverter<BsonMinKey>>().Object,
                new Mock<IJsonConverter<BsonNull>>().Object,
                new Mock<IJsonConverter<ObjectId>>().Object,
                new Mock<IJsonConverter<BsonRegularExpression>>().Object,
                new Mock<IJsonConverter<string>>().Object,
                new Mock<IJsonConverter<string>>().Object,
                new Mock<IJsonConverter<long>>().Object,
                new Mock<IJsonConverter<BsonUndefined>>().Object);
        }

        private List<string> FindDifferences(JsonConverterSet x, JsonConverterSet y)
        {
            var result = new List<string>();
            if (x.BinaryDataConverter != y.BinaryDataConverter) { result.Add("BinaryDataConverter"); }
            if (x.BooleanConverter != y.BooleanConverter) { result.Add("BooleanConverter"); }
            if (x.DateTimeConverter != y.DateTimeConverter) { result.Add("DateTimeConverter"); }
            if (x.Decimal128Converter != y.Decimal128Converter) { result.Add("Decimal128Converter"); }
            if (x.DoubleConverter != y.DoubleConverter) { result.Add("DoubleConverter"); }
            if (x.Int32Converter != y.Int32Converter) { result.Add("Int32Converter"); }
            if (x.Int64Converter != y.Int64Converter) { result.Add("Int64Converter"); }
            if (x.JavaScriptConverter != y.JavaScriptConverter) { result.Add("JavaScriptConverter"); }
            if (x.MaxKeyConverter != y.MaxKeyConverter) { result.Add("MaxKeyConverter"); }
            if (x.MinKeyConverter != y.MinKeyConverter) { result.Add("MinKeyConverter"); }
            if (x.NullConverter != y.NullConverter) { result.Add("NullConverter"); }
            if (x.ObjectIdConverter != y.ObjectIdConverter) { result.Add("ObjectIdConverter"); }
            if (x.RegularExpressionConverter != y.RegularExpressionConverter) { result.Add("RegularExpressionConverter"); }
            if (x.StringConverter != y.StringConverter) { result.Add("StringConverter"); }
            if (x.SymbolConverter != y.SymbolConverter) { result.Add("SymbolConverter"); }
            if (x.TimestampConverter != y.TimestampConverter) { result.Add("TimestampConverter"); }
            if (x.UndefinedConverter != y.UndefinedConverter) { result.Add("UndefinedConverter"); }
            return result;
        }
    }
}
