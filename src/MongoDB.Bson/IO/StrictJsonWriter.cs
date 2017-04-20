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
using System.Globalization;
using System.IO;
using System.Text;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a strict JSON writer that writes to a TextWriter.
    /// </summary>
    /// <seealso cref="MongoDB.Bson.IO.IStrictJsonWriter" />
    public class StrictJsonWriter : IStrictJsonWriter
    {
        // private fields
        private Context _context;
        private readonly StrictJsonWriterSettings _settings;
        private State _state;
        private readonly TextWriter _writer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="StrictJsonWriter"/> class.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="settings">The settings.</param>
        public StrictJsonWriter(TextWriter writer, StrictJsonWriterSettings settings)
        {
            if (writer == null) { throw new ArgumentNullException(nameof(writer)); }
            if (settings == null) { throw new ArgumentNullException(nameof(settings)); }
            _writer = writer;
            _settings = settings;
            _context = new Context(null, ContextType.TopLevel, "");
            _state = State.Value;
        }

        // public properties
        /// <summary>
        /// Gets the settings.
        /// </summary>
        public StrictJsonWriterSettings Settings => _settings;

        /// <summary>
        /// Gets the text writer.
        /// </summary>
        public TextWriter TextWriter => _writer;

        // public methods
        /// <inheritdoc/>
        public void WriteBoolean(bool value)
        {
            EnsureValueState(nameof(WriteBoolean));
            WriteValue(JsonConvert.ToString(value));
        }

        /// <inheritdoc/>
        public void WriteDouble(double value)
        {
            EnsureValueState(nameof(WriteDouble));
            WriteValue(JsonConvert.ToString(value));
        }

        /// <inheritdoc/>
        public void WriteEndArray()
        {
            if (_context.Type != ContextType.Array)
            {
                throw new InvalidOperationException("WriteEndArray called when not expected.");
            }
            EnsureValueState(nameof(WriteEndArray));

            _writer.Write("]");
            _context = _context.Parent;
            _state = _context.Type == ContextType.Document ? State.Name : State.Value;
        }

        /// <inheritdoc/>
        public void WriteEndDocument()
        {
            if (_context.Type != ContextType.Document)
            {
                throw new InvalidOperationException("WriteEndDocument called when not expected.");
            }
            EnsureNameState(nameof(WriteStartDocument));

            var sb = new StringBuilder();
            if (_settings.Indent && _context.HasElements)
            {
                sb.Append(_settings.NewLineChars);
                sb.Append(_context.Parent.Indentation);
                sb.Append("}");
            }
            else
            {
                sb.Append(" }");
            }

            _writer.Write(sb.ToString());
            _context = _context.Parent;
            _state = _context.Type == ContextType.Document ? State.Name : State.Value;

        }

        /// <inheritdoc/>
        public void WriteInt32(int value)
        {
            EnsureValueState(nameof(WriteInt32));
            WriteValue(JsonConvert.ToString(value));
        }

        /// <inheritdoc/>
        public void WriteInt64(long value)
        {
            EnsureValueState(nameof(WriteInt64));
            WriteValue(JsonConvert.ToString(value));
        }

        /// <inheritdoc/>
        public void WriteName(string name)
        {
            if (name == null) { throw new ArgumentNullException(nameof(name)); }
            EnsureNameState(nameof(WriteName));

            var sb = new StringBuilder();

            if (_context.HasElements)
            {
                sb.Append(",");
            }

            if (_settings.Indent)
            {
                sb.Append(_settings.NewLineChars);
                sb.Append(_context.Indentation);
            }
            else
            {
                sb.Append(" ");
            }

            if (_settings.AlwaysQuoteNames || NameNeedsToBeQuoted(name))
            {
                sb.Append(QuoteString(name));
            }
            else
            {
                sb.Append(name);
            }

            sb.Append(" : ");

            _writer.Write(sb.ToString());
            _state = State.Value;
        }

        /// <inheritdoc/>
        public void WriteNull()
        {
            EnsureValueState(nameof(WriteNull));
            WriteValue("null");
        }

        /// <inheritdoc/>
        public void WriteStartArray()
        {
            EnsureValueState(nameof(WriteStartArray));
            PrepareToWriteValue();
            _writer.Write("[");
            _context = new Context(_context, ContextType.Array, _settings.IndentChars);
            _state = State.Value;
        }

        /// <inheritdoc/>
        public void WriteStartDocument()
        {
            EnsureValueState(nameof(WriteStartDocument));
            PrepareToWriteValue();
            _writer.Write("{");
            _context = new Context(_context, ContextType.Document, _settings.IndentChars);
            _state = State.Name;
        }

        /// <inheritdoc/>
        public void WriteString(string value)
        {
            EnsureValueState(nameof(WriteString));
            WriteValue(QuoteString(value));
        }

        /// <inheritdoc/>
        public void WriteValue(string representation)
        {
            if (representation == null) { throw new ArgumentNullException(nameof(representation)); }
            EnsureValueState(nameof(WriteValue));
            PrepareToWriteValue();
            _writer.Write(representation);
            if (_context.Type == ContextType.Document)
            {
                _state = State.Name;
            }
        }

        // private methods
        private void EnsureNameState(string methodName)
        {
            if (_state != State.Name)
            {
                throw new InvalidOperationException($"{methodName} was called when the writer was not expecting a name.");
            }
        }

        private void EnsureValueState(string methodName)
        {
            if (_state != State.Value)
            {
                throw new InvalidOperationException($"{methodName} was called when the writer was not expecting a value.");
            }
        }

        private bool NameNeedsToBeQuoted(string value)
        {
            if (value.Length == 0 || char.IsDigit(value[0]))
            {
                return true;
            }

            foreach (var c in value)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    return true;
                }
            }

            return false;
        }

        private void PrepareToWriteValue()
        {
            if (_context.Type == ContextType.Array && _context.HasElements)
            {
                _writer.Write(", ");
            }
            _context.HasElements = true;
        }

        private string QuoteString(string value)
        {
            var sb = new StringBuilder(value.Length + 2);

            sb.Append('"');
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        switch (CharUnicodeInfo.GetUnicodeCategory(c))
                        {
                            case UnicodeCategory.ClosePunctuation:
                            case UnicodeCategory.ConnectorPunctuation:
                            case UnicodeCategory.CurrencySymbol:
                            case UnicodeCategory.DashPunctuation:
                            case UnicodeCategory.DecimalDigitNumber:
                            case UnicodeCategory.FinalQuotePunctuation:
                            case UnicodeCategory.InitialQuotePunctuation:
                            case UnicodeCategory.LetterNumber:
                            case UnicodeCategory.LowercaseLetter:
                            case UnicodeCategory.MathSymbol:
                            case UnicodeCategory.ModifierSymbol:
                            case UnicodeCategory.OpenPunctuation:
                            case UnicodeCategory.OtherLetter:
                            case UnicodeCategory.OtherNumber:
                            case UnicodeCategory.OtherPunctuation:
                            case UnicodeCategory.OtherSymbol:
                            case UnicodeCategory.SpaceSeparator:
                            case UnicodeCategory.TitlecaseLetter:
                            case UnicodeCategory.UppercaseLetter:
                                sb.Append(c);
                                break;

                            default:
                                sb.AppendFormat("\\u{0:x4}", (int)c);
                                break;
                        }
                        break;
                }
            }
            sb.Append('"');

            return sb.ToString();
        }

        // nested types
        private class Context
        {
            public Context(Context parent, ContextType type, string indentChars)
            {
                Parent = parent;
                Type = type;
                Indentation = (parent?.Indentation ?? "") + indentChars;
            }

            public bool HasElements { get; set; }
            public string Indentation { get; }
            public Context Parent { get; }
            public ContextType Type { get; }
        }

        private enum ContextType
        {
            TopLevel,
            Document,
            Array
        }

        private enum State
        {
            Name,
            Value
        }
    }
}
