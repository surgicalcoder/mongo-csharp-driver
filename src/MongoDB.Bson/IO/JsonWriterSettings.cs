/* Copyright 2010-2017 MongoDB Inc.
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
    /// Represents settings for a JsonWriter.
    /// </summary>
#if NET45
    [Serializable]
#endif
    public class JsonWriterSettings : BsonWriterSettings
    {
        // private static fields
        private static JsonWriterSettings __defaults = null; // delay creation to pick up the latest default values

        // private fields
        private bool _alwaysQuoteNames = true;
        private JsonConverterSet _converters = JsonConverterSet.ShellJsonConverters;
        private Encoding _encoding = Encoding.UTF8;
        private bool _indent = false;
        private string _indentChars = "  ";
        private string _newLineChars = "\r\n";
        private JsonOutputMode _outputMode = JsonOutputMode.Shell;
        private Version _shellVersion;

        // constructors
        /// <summary>
        /// Initializes a new instance of the JsonWriterSettings class.
        /// </summary>
        public JsonWriterSettings()
        {
        }

        // public static properties
        /// <summary>
        /// Gets or sets the default JsonWriterSettings.
        /// </summary>
        public static JsonWriterSettings Defaults
        {
            get
            {
                if (__defaults == null)
                {
                    __defaults = new JsonWriterSettings();
                    __defaults.Freeze();
                }
                return __defaults;
            }
            set
            {
                if (value == null) { throw new ArgumentNullException(nameof(value)); }
                value.Freeze();
                __defaults = value;
            }
        }

        // public properties
        /// <summary>
        /// Gets or sets a value indicating whether to always quote names.
        /// </summary>
        public bool AlwaysQuoteNames
        {
            get { return _alwaysQuoteNames; }
            set
            {
                if (IsFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen."); }
                _alwaysQuoteNames = value;
            }
        }

        /// <summary>
        /// Gets or sets the converters.
        /// </summary>
        public JsonConverterSet Converters
        {
            get { return _converters; }
            set
            {
                if (value == null) { throw new ArgumentNullException(nameof(value)); }
                if (IsFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen."); }
                _converters = value;
            }
        }

        /// <summary>
        /// Gets or sets the output Encoding.
        /// </summary>
        [Obsolete("Set the Encoding when you create a StreamWriter instead (this property is ignored).")]
        public Encoding Encoding
        {
            get { return _encoding; }
            set
            {
                if (IsFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen."); }
                _encoding = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to indent the output.
        /// </summary>
        public bool Indent
        {
            get { return _indent; }
            set
            {
                if (IsFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen."); }
                _indent = value;
            }
        }

        /// <summary>
        /// Gets or sets the indent characters.
        /// </summary>
        public string IndentChars
        {
            get { return _indentChars; }
            set
            {
                if (value == null) { throw new ArgumentNullException(nameof(value)); }
                if (IsFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen."); }
                _indentChars = value;
            }
        }

        /// <summary>
        /// Gets or sets the new line characters.
        /// </summary>
        public string NewLineChars
        {
            get { return _newLineChars; }
            set
            {
                if (value == null) { throw new ArgumentNullException(nameof(value)); }
                if (IsFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen."); }
                _newLineChars = value;
            }
        }

        /// <summary>
        /// Gets or sets the output mode.
        /// </summary>
        public JsonOutputMode OutputMode
        {
            get { return _outputMode; }
            set
            {
                if (IsFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen."); }
                _outputMode = value;
                switch (value)
                {
                    case JsonOutputMode.Strict:
                        _converters = JsonConverterSet.StrictJsonConverters;
                        break;

                    case JsonOutputMode.Shell:
                    default:
                        _converters = JsonConverterSet.ShellJsonConverters;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets or sets the shell version (used with OutputMode Shell).
        /// </summary>
        public Version ShellVersion
        {
            get { return _shellVersion; }
            set
            {
                if (IsFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen."); }
                _shellVersion = value;
            }
        }

        // public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public new JsonWriterSettings Clone()
        {
            return (JsonWriterSettings)CloneImplementation();
        }

        /// <summary>
        /// Creates a StrictJsonWriterSettings.
        /// </summary>
        /// <returns>A StrictJsonWriterSettings.</returns>
        public StrictJsonWriterSettings ToStrictJsonWriterSettings()
        {
            return new StrictJsonWriterSettings(
                _alwaysQuoteNames,
                _indent,
                _indentChars,
                _newLineChars);
        }

        // protected methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        protected override BsonWriterSettings CloneImplementation()
        {
            var clone = new JsonWriterSettings
            {
                AlwaysQuoteNames = _alwaysQuoteNames,
                Converters = _converters,
#pragma warning disable 618
                Encoding = _encoding,
#pragma warning restore
                GuidRepresentation = GuidRepresentation,
                Indent = _indent,
                IndentChars = _indentChars,
                MaxSerializationDepth = MaxSerializationDepth,
                NewLineChars = _newLineChars,
                OutputMode = _outputMode,
                ShellVersion = _shellVersion
            };
            return clone;
        }
    }
}
