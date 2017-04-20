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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.IO;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class StrictJsonWriterTests
    {

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var writer = new StringWriter();
            var settings = CreateSettings();

            var result = new StrictJsonWriter(writer, settings);

            result.TextWriter.Should().BeSameAs(writer);
            result.Settings.Should().BeSameAs(settings);
        }

        [Fact]
        public void constructor_should_throw_when_writer_is_null()
        {
            var settings = CreateSettings();

            var exception = Record.Exception(() => new StrictJsonWriter(null, settings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("writer");
        }

        [Fact]
        public void constructor_should_throw_when_settings_is_null()
        {
            var writer = new StringWriter();

            var exception = Record.Exception(() => new StrictJsonWriter(writer, null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("settings");
        }

        [Theory]
        [InlineData(false, "{ \"x\" : false }")]
        [InlineData(true, "{ \"x\" : true }")]
        public void WriteBoolean_should_have_expected_result(bool value, string expectedResult)
        {
            var subject = CreateSubject();

            WriteDocument(subject, () => subject.WriteBoolean(value));

            OutputResult(subject).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0.0, "{ \"x\" : 0 }")]
        [InlineData(0.5, "{ \"x\" : 0.5 }")]
        public void WriteDouble_should_have_expected_result(double value, string expectedResult)
        {
            var subject = CreateSubject();

            WriteDocument(subject, () => subject.WriteDouble(value));

            OutputResult(subject).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0, "{ \"x\" : 0 }")]
        [InlineData(1, "{ \"x\" : 1 }")]
        public void WriteInt32_should_have_expected_result(int value, string expectedResult)
        {
            var subject = CreateSubject();

            WriteDocument(subject, () => subject.WriteInt32(value));

            OutputResult(subject).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0, "{ \"x\" : 0 }")]
        [InlineData(1, "{ \"x\" : 1 }")]
        public void WriteInt64_should_have_expected_result(long value, string expectedResult)
        {
            var subject = CreateSubject();

            WriteDocument(subject, () => subject.WriteInt64(value));

            OutputResult(subject).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("x", false, "{ x : 0 }")]
        [InlineData("x", true, "{ \"x\" : 0 }")]
        [InlineData("", false, "{ \"\" : 0 }")]
        [InlineData("0", false, "{ \"0\" : 0 }")]
        [InlineData("0x", false, "{ \"0x\" : 0 }")]
        [InlineData("_x", false, "{ _x : 0 }")]
        [InlineData("_0", false, "{ _0 : 0 }")]
        public void WriteName_should_have_expected_result(string name, bool alwaysQuoteNames, string expectedResult)
        {
            var subject = CreateSubject(alwaysQuoteNames: alwaysQuoteNames);

            subject.WriteStartDocument();
            subject.WriteName(name);
            subject.WriteInt32(0);
            subject.WriteEndDocument();

            OutputResult(subject).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("{ \"x\" : null }")]
        public void WriteNull_should_have_expected_result(string expectedResult)
        {
            var subject = CreateSubject();

            WriteDocument(subject, () => subject.WriteNull());

            OutputResult(subject).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("", "{ \"x\" : \"\" }")]
        [InlineData("a", "{ \"x\" : \"a\" }")]
        [InlineData("abc", "{ \"x\" : \"abc\" }")]
        [InlineData("\" \\ \b \f \n \r \t \uf123", "{ \"x\" : \"\\\" \\\\ \\b \\f \\n \\r \\t \\uf123\" }")]
        public void WriteString_should_have_expected_result(string value, string expectedResult)
        {
            var subject = CreateSubject();

            WriteDocument(subject, () => subject.WriteString(value));

            OutputResult(subject).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("abc", "{ \"x\" : abc }")]
        [InlineData("def", "{ \"x\" : def }")]
        public void WriteValue_should_have_expected_result(string representation, string expectedResult)
        {
            var subject = CreateSubject();

            WriteDocument(subject, () => subject.WriteValue(representation));

            OutputResult(subject).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, "{ \"x\" : [] }")]
        [InlineData(true, "{\r\n  \"x\" : []\r\n}")]
        public void WriteArray_with_0_items_should_have_expected_result(bool indent, string expectedResult)
        {
            var subject = CreateSubject(indent: indent);

            subject.WriteStartDocument();
            subject.WriteName("x");
            subject.WriteStartArray();
            subject.WriteEndArray();
            subject.WriteEndDocument();

            OutputResult(subject).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, "{ \"x\" : [0] }")]
        [InlineData(true, "{\r\n  \"x\" : [0]\r\n}")]
        public void WriteArray_with_1_item_should_have_expected_result(bool indent, string expectedResult)
        {
            var subject = CreateSubject(indent: indent);

            subject.WriteStartDocument();
            subject.WriteName("x");
            subject.WriteStartArray();
            subject.WriteInt32(0);
            subject.WriteEndArray();
            subject.WriteEndDocument();

            OutputResult(subject).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, "{ \"x\" : [1, 2] }")]
        [InlineData(true, "{\r\n  \"x\" : [1, 2]\r\n}")]
        public void WriteArray_with_2_items_should_have_expected_result(bool indent, string expectedResult)
        {
            var subject = CreateSubject(indent: indent);

            subject.WriteStartDocument();
            subject.WriteName("x");
            subject.WriteStartArray();
            subject.WriteInt32(1);
            subject.WriteInt32(2);
            subject.WriteEndArray();
            subject.WriteEndDocument();

            OutputResult(subject).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, "{ \"x\" : [1, [2, 3]] }")]
        [InlineData(true, "{\r\n  \"x\" : [1, [2, 3]]\r\n}")]
        public void WriteArray_with_nested_array_should_have_expected_result(bool indent, string expectedResult)
        {
            var subject = CreateSubject(indent: indent);

            subject.WriteStartDocument();
            subject.WriteName("x");
            subject.WriteStartArray();
            subject.WriteInt32(1);
            subject.WriteStartArray();
            subject.WriteInt32(2);
            subject.WriteInt32(3);
            subject.WriteEndArray();
            subject.WriteEndArray();
            subject.WriteEndDocument();

            OutputResult(subject).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, "{ }")]
        [InlineData(true, "{ }")]
        public void WriteDocument_with_0_elements_should_have_expected_result(bool indent, string expectedResult)
        {
            var subject = CreateSubject(indent: indent);

            subject.WriteStartDocument();
            subject.WriteEndDocument();

            OutputResult(subject).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, "{ \"x\" : 0 }")]
        [InlineData(true, "{\r\n  \"x\" : 0\r\n}")]
        public void WriteDocument_with_1_element_should_have_expected_result(bool indent, string expectedResult)
        {
            var subject = CreateSubject(indent: indent);

            subject.WriteStartDocument();
            subject.WriteName("x");
            subject.WriteInt32(0);
            subject.WriteEndDocument();

            OutputResult(subject).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, "{ \"x\" : 1, \"y\" : 2 }")]
        [InlineData(true, "{\r\n  \"x\" : 1,\r\n  \"y\" : 2\r\n}")]
        public void WriteDocument_with_2_elements_should_have_expected_result(bool indent, string expectedResult)
        {
            var subject = CreateSubject(indent: indent);

            subject.WriteStartDocument();
            subject.WriteName("x");
            subject.WriteInt32(1);
            subject.WriteName("y");
            subject.WriteInt32(2);
            subject.WriteEndDocument();

            OutputResult(subject).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, "{ \"x\" : 1, \"y\" : { \"z\" : 2 } }")]
        [InlineData(true, "{\r\n  \"x\" : 1,\r\n  \"y\" : {\r\n    \"z\" : 2\r\n  }\r\n}")]
        public void WriteDocument_with_nested_document_should_have_expected_result(bool indent, string expectedResult)
        {
            var subject = CreateSubject(indent: indent);

            subject.WriteStartDocument();
            subject.WriteName("x");
            subject.WriteInt32(1);
            subject.WriteName("y");
            subject.WriteStartDocument();
            subject.WriteName("z");
            subject.WriteInt32(2);
            subject.WriteEndDocument();
            subject.WriteEndDocument();

            OutputResult(subject).Should().Be(expectedResult);
        }

        // private methods
        private StrictJsonWriterSettings CreateSettings(
            bool alwaysQuoteNames = true,
            bool indent = false)
        {
            return new StrictJsonWriterSettings(
                alwaysQuoteNames: alwaysQuoteNames,
                indent: indent,
                indentChars: "  ",
                newLineChars: "\r\n");
        }

        private StrictJsonWriter CreateSubject(
            bool alwaysQuoteNames = true,
            bool indent = false)
        {
            var writer = new StringWriter();
            var settings = CreateSettings(alwaysQuoteNames: alwaysQuoteNames, indent: indent);
            return new StrictJsonWriter(writer, settings);
        }

        private string OutputResult(StrictJsonWriter writer)
        {
            return ((StringWriter)writer.TextWriter).ToString();
        }

        private void WriteDocument(StrictJsonWriter writer, Action action)
        {
            writer.WriteStartDocument();
            writer.WriteName("x");
            action();
            writer.WriteEndDocument();
        }
    }
}
