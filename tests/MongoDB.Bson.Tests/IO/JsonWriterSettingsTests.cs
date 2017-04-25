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
using System.Text;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.IO.JsonConverters;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class JsonWriterSettingsTests
    {
        [Fact]
        public void Defaults_get_should_return_expected_result()
        {
#pragma warning disable 618
            var result = JsonWriterSettings.Defaults;

            result.AlwaysQuoteNames.Should().BeTrue();
            result.OutputConverters.Should().BeSameAs(JsonOutputConverters.Shell);
            result.Encoding.Should().BeOfType<UTF8Encoding>();
            result.GuidRepresentation.Should().Be(GuidRepresentation.CSharpLegacy);
            result.Indent.Should().BeFalse();
            result.IndentChars.Should().Be("  ");
            result.IsFrozen.Should().BeTrue();
            result.MaxSerializationDepth.Should().Be(100);
            result.NewLineChars.Should().Be("\r\n");
            result.OutputMode.Should().Be(JsonOutputMode.Shell);
            result.ShellVersion.Should().BeNull();
#pragma warning restore
        }

        [Fact]
        public void Defaults_get_should_return_same_instance()
        {
            var result1 = JsonWriterSettings.Defaults;
            var result2 = JsonWriterSettings.Defaults;

            result2.Should().BeSameAs(result1);
        }

        [Fact]
        public void Defaults_set_should_have_expected_result()
        {
            var value = new JsonWriterSettings();

            JsonWriterSettings.Defaults = value;
            var result = JsonWriterSettings.Defaults;

            result.Should().BeSameAs(value);
            result.IsFrozen.Should().BeTrue();
        }

        [Fact]
        public void Defaults_set_should_throw_when_value_is_null()
        {
            var exception = Record.Exception(() => { JsonWriterSettings.Defaults = null; });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("value");
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
#pragma warning disable 618
            var result = new JsonWriterSettings();

            result.AlwaysQuoteNames.Should().BeTrue();
            result.OutputConverters.Should().BeSameAs(JsonOutputConverters.Shell);
            result.Encoding.Should().BeOfType<UTF8Encoding>();
            result.GuidRepresentation.Should().Be(GuidRepresentation.CSharpLegacy);
            result.Indent.Should().BeFalse();
            result.IndentChars.Should().Be("  ");
            result.IsFrozen.Should().BeFalse();
            result.MaxSerializationDepth.Should().Be(100);
            result.NewLineChars.Should().Be("\r\n");
            result.OutputMode.Should().Be(JsonOutputMode.Shell);
            result.ShellVersion.Should().BeNull();
#pragma warning restore
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AlwaysQuoteNames_set_should_have_expected_result(bool value)
        {
            var subject = new JsonWriterSettings();

            subject.AlwaysQuoteNames = value;
            var result = subject.AlwaysQuoteNames;

            result.Should().Be(value);
        }

        [Fact]
        public void AlwaysQuoteNames_set_should_throw_when_subject_is_frozen()
        {
            var subject = CreateFrozenSubject();

            var exception = Record.Exception(() => { subject.AlwaysQuoteNames = true; });

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void Converters_set_should_have_expected_result()
        {
            var subject = new JsonWriterSettings();
            var value = CreateConverterSet();

            subject.OutputConverters = value;
            var result = subject.OutputConverters;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void Converters_set_should_throw_when_subject_is_frozen()
        {
            var subject = CreateFrozenSubject();
            var value = CreateConverterSet();

            var exception = Record.Exception(() => { subject.OutputConverters = value; });

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void Converters_set_should_throw_when_value_is_null()
        {
            var subject = CreateFrozenSubject();

            var exception = Record.Exception(() => { subject.OutputConverters = null; });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("value");
        }

        [Fact]
        public void Encoding_set_should_have_expected_result()
        {
#pragma warning disable 618
            var subject = new JsonWriterSettings();
            var value = new UTF8Encoding();

            subject.Encoding = value;
            var result = subject.Encoding;

            result.Should().BeSameAs(value);
#pragma warning restore
        }

        [Fact]
        public void Encoding_set_should_throw_when_subject_is_frozen()
        {
#pragma warning disable 618
            var subject = CreateFrozenSubject();
            var value = new UTF8Encoding();

            var exception = Record.Exception(() => { subject.Encoding = value; });

            exception.Should().BeOfType<InvalidOperationException>();
#pragma warning restore
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Indent_set_should_have_expected_result(bool value)
        {
            var subject = new JsonWriterSettings();

            subject.Indent = value;
            var result = subject.Indent;

            result.Should().Be(value);
        }

        [Fact]
        public void Indent_set_should_throw_when_subject_is_frozen()
        {
            var subject = CreateFrozenSubject();

            var exception = Record.Exception(() => { subject.AlwaysQuoteNames = true; });

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("  ")]
        public void IndentChars_set_should_have_expected_result(string value)
        {
            var subject = new JsonWriterSettings();

            subject.IndentChars = value;
            var result = subject.IndentChars;

            result.Should().Be(value);
        }

        [Fact]
        public void IndentChars_set_should_throw_when_subject_is_frozen()
        {
            var subject = CreateFrozenSubject();

            var exception = Record.Exception(() => { subject.IndentChars = ""; });

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void IndentChars_set_should_throw_when_value_is_null()
        {
            var subject = CreateFrozenSubject();

            var exception = Record.Exception(() => { subject.IndentChars = null; });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("value");
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void NewLineChars_set_should_have_expected_result(string value)
        {
            var subject = new JsonWriterSettings();

            subject.NewLineChars = value;
            var result = subject.NewLineChars;

            result.Should().Be(value);
        }

        [Fact]
        public void NewLineChars_set_should_throw_when_subject_is_frozen()
        {
            var subject = CreateFrozenSubject();

            var exception = Record.Exception(() => { subject.NewLineChars = ""; });

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void NewLineChars_set_should_throw_when_value_is_null()
        {
            var subject = CreateFrozenSubject();

            var exception = Record.Exception(() => { subject.NewLineChars = null; });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("value");
        }

        [Theory]
        [InlineData(JsonOutputMode.Shell)]
        [InlineData(JsonOutputMode.Strict)]
        public void OutputMode_set_should_have_expected_result(JsonOutputMode value)
        {
#pragma warning disable 618
            var subject = new JsonWriterSettings();
            subject.OutputConverters = CreateConverterSet();

            subject.OutputMode = value;
            var result = subject.OutputMode;

            result.Should().Be(value);
            subject.OutputConverters.Should().BeSameAs(value == JsonOutputMode.Strict ? JsonOutputConverters.Strict : JsonOutputConverters.Shell);
#pragma warning restore
        }

        [Fact]
        public void ShellVersion_set_should_have_expected_result()
        {
            var subject = new JsonWriterSettings();
            var value = new Version(1, 2, 3);

            subject.ShellVersion = value;
            var result = subject.ShellVersion;

            result.Should().Be(value);
        }

        [Fact]
        public void ShellVersion_set_should_throw_when_subject_is_frozen()
        {
            var subject = CreateFrozenSubject();
            var value = new Version(1, 2, 3);

            var exception = Record.Exception(() => { subject.ShellVersion = value; });

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void Clone_should_return_expected_result()
        {
#pragma warning disable 618
            var subject = new JsonWriterSettings();
            subject.AlwaysQuoteNames = !subject.AlwaysQuoteNames;
            subject.Encoding = new UTF8Encoding();
            subject.GuidRepresentation = (GuidRepresentation)((int)subject.GuidRepresentation + 1);
            subject.Indent = !subject.Indent;
            subject.IndentChars = subject.IndentChars + " ";
            subject.MaxSerializationDepth = subject.MaxSerializationDepth + 1;
            subject.NewLineChars = subject.NewLineChars + "\r\n";
            subject.OutputMode = subject.OutputMode == JsonOutputMode.Shell ? JsonOutputMode.Strict : JsonOutputMode.Shell; // set OutputMode before OutputConverters
            subject.OutputConverters = subject.OutputConverters == JsonOutputConverters.Shell ? JsonOutputConverters.Strict : JsonOutputConverters.Shell;
            subject.ShellVersion = new Version(1, 3, 2);
            subject.Freeze();

            var result = subject.Clone();

            Equals(result, subject).Should().BeTrue();
            result.IsFrozen.Should().BeFalse();
#pragma warning restore
        }

        [Fact]
        public void ToStrictJsonWriterSettings_should_return_expected_result()
        {
#pragma warning disable 618
            var subject = new JsonWriterSettings();
            subject.AlwaysQuoteNames = !subject.AlwaysQuoteNames;
            subject.Encoding = new UTF8Encoding();
            subject.Indent = !subject.Indent;
            subject.IndentChars = subject.IndentChars + " ";
            subject.NewLineChars = subject.NewLineChars + "\r\n";
            subject.Freeze();

            var result = subject.ToStrictJsonWriterSettings();

            result.AlwaysQuoteNames.Should().Be(subject.AlwaysQuoteNames);
            result.Indent.Should().Be(subject.Indent);
            result.IndentChars.Should().Be(subject.IndentChars);
            result.NewLineChars.Should().Be(subject.NewLineChars);
#pragma warning restore
        }

        // private methods
        private JsonOutputConverterSet CreateConverterSet()
        {
            var doubleConverter = new DoubleStrictJsonConverter();
            return JsonOutputConverters.Shell.With(doubleConverter: doubleConverter);
        }

        private JsonWriterSettings CreateFrozenSubject()
        {
            var subject = new JsonWriterSettings();
            subject.Freeze();
            return subject;
        }

        private bool Equals(JsonWriterSettings x, JsonWriterSettings y)
        {
#pragma warning disable 618
            return
                x.AlwaysQuoteNames == y.AlwaysQuoteNames &&
                x.OutputConverters == y.OutputConverters &&
                x.Encoding == y.Encoding &&
                x.GuidRepresentation == y.GuidRepresentation &&
                x.Indent == y.Indent &&
                x.IndentChars == y.IndentChars &&
                x.MaxSerializationDepth == y.MaxSerializationDepth &&
                x.NewLineChars == y.NewLineChars &&
                x.OutputMode == y.OutputMode &&
                x.ShellVersion == y.ShellVersion;
#pragma warning restore
        }
    }
}
