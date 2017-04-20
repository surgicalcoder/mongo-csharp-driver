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
using FluentAssertions;
using MongoDB.Bson.IO;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class StrictJsonWriterSettingsTests
    {
        [Theory]
        [InlineData(false, false, "", "")]
        [InlineData(true, true, "  ", "\r\n")]
        public void constructor_should_initialize_instance(
            bool alwaysQuoteNames,
            bool indent,
            string indentChars,
            string newLineChars)
        {
            var result = new StrictJsonWriterSettings(alwaysQuoteNames, indent, indentChars, newLineChars);

            result.AlwaysQuoteNames.Should().Be(alwaysQuoteNames);
            result.Indent.Should().Be(indent);
            result.IndentChars.Should().Be(indentChars);
            result.NewLineChars.Should().Be(newLineChars);
        }

        [Fact]
        public void constructor_should_throw_when_indentChars_is_null()
        {
            var exception = Record.Exception(() => new StrictJsonWriterSettings(false, false, null, ""));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("indentChars");
        }

        [Fact]
        public void constructor_should_throw_when_newLineChars_is_null()
        {
            var exception = Record.Exception(() => new StrictJsonWriterSettings(false, false, "", null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("newLineChars");
        }
    }
}
