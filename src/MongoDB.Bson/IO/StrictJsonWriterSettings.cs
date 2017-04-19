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

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents settings for a StrictJsonWriter.
    /// </summary>
    public class StrictJsonWriterSettings
    {
        // private fields
        private readonly bool _alwaysQuoteNames;
        private readonly Encoding _encoding;
        private readonly bool _indent;
        private readonly string _indentChars;
        private readonly string _newLineChars;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="StrictJsonWriterSettings" /> class.
        /// </summary>
        /// <param name="alwaysQuoteNames">If set to <c>true</c> always quote names.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="indent">if set to <c>true</c> [indent].</param>
        /// <param name="indentChars">The indent chars.</param>
        /// <param name="newLineChars">The new line chars.</param>
        public StrictJsonWriterSettings(
            bool alwaysQuoteNames,
            Encoding encoding,
            bool indent,
            string indentChars,
            string newLineChars)
        {
            if (encoding == null) { throw new ArgumentNullException(nameof(encoding)); }
            if (indentChars == null) { throw new ArgumentNullException(nameof(indentChars)); }
            if (newLineChars == null) { throw new ArgumentNullException(nameof(newLineChars)); }
            _alwaysQuoteNames = alwaysQuoteNames;
            _encoding = encoding;
            _indent = indent;
            _indentChars = indentChars;
            _newLineChars = newLineChars;
        }

        // public properties
        /// <summary>
        /// Gets a value indicating whether to always quote names.
        /// </summary>
        public bool AlwaysQuoteNames => _alwaysQuoteNames;

        /// <summary>
        /// Gets the encoding.
        /// </summary>
        public Encoding Encoding => _encoding;

        /// <summary>
        /// Gets a value indicating whether the JSON output should be indented.
        /// </summary>
        public bool Indent => _indent;

        /// <summary>
        /// Gets the indentation characters.
        /// </summary>
        public string IndentChars => _indentChars;

        /// <summary>
        /// Gets the new line characters.
        /// </summary>
        public string NewLineChars => _newLineChars;
    }
}
