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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a BSON writer to a TextWriter (in JSON format).
    /// </summary>
    public class JsonWriter : BsonWriter
    {
        // private fields
        private TextWriter _textWriter;
        private JsonWriterSettings _jsonWriterSettings; // same value as in base class just declared as derived class
        private JsonWriterContext _context;
        private readonly IStrictJsonWriter _strictWriter;

        // constructors
        /// <summary>
        /// Initializes a new instance of the JsonWriter class.
        /// </summary>
        /// <param name="writer">A TextWriter.</param>
        public JsonWriter(TextWriter writer)
            : this(writer, JsonWriterSettings.Defaults)
        {
        }

        /// <summary>
        /// Initializes a new instance of the JsonWriter class.
        /// </summary>
        /// <param name="writer">A TextWriter.</param>
        /// <param name="settings">Optional JsonWriter settings.</param>
        public JsonWriter(TextWriter writer, JsonWriterSettings settings)
            : base(settings)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            _textWriter = writer;
            _strictWriter = new StrictJsonWriter(writer, settings.ToStrictJsonWriterSettings());
            _jsonWriterSettings = settings; // already frozen by base class
            _context = new JsonWriterContext(null, ContextType.TopLevel, "");
            State = BsonWriterState.Initial;
        }

        // public properties
        /// <summary>
        /// Gets the base TextWriter.
        /// </summary>
        /// <value>
        /// The base TextWriter.
        /// </value>
        public TextWriter BaseTextWriter
        {
            get { return _textWriter; }
        }

        // public methods
        /// <summary>
        /// Closes the writer.
        /// </summary>
        public override void Close()
        {
            // Close can be called on Disposed objects
            if (State != BsonWriterState.Closed)
            {
                Flush();
                _context = null;
                State = BsonWriterState.Closed;
            }
        }

        /// <summary>
        /// Flushes any pending data to the output destination.
        /// </summary>
        public override void Flush()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            _textWriter.Flush();
        }

        /// <summary>
        /// Writes BSON binary data to the writer.
        /// </summary>
        /// <param name="binaryData">The binary data.</param>
        public override void WriteBinaryData(BsonBinaryData binaryData)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteBinaryData", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.BinaryDataConverter.Convert(binaryData, _strictWriter);
            _context.HasElements = true;

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON Boolean to the writer.
        /// </summary>
        /// <param name="value">The Boolean value.</param>
        public override void WriteBoolean(bool value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteBoolean", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.BooleanConverter.Convert(value, _strictWriter);
            _context.HasElements = true;

            State = GetNextState();
        }

        /// <summary>
        /// Writes BSON binary data to the writer.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        public override void WriteBytes(byte[] bytes)
        {
            WriteBinaryData(new BsonBinaryData(bytes, BsonBinarySubType.Binary));
        }

        /// <summary>
        /// Writes a BSON DateTime to the writer.
        /// </summary>
        /// <param name="value">The number of milliseconds since the Unix epoch.</param>
        public override void WriteDateTime(long value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteDateTime", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.DateTimeConverter.Convert(value, _strictWriter);
            _context.HasElements = true;

            State = GetNextState();
        }

        /// <inheritdoc />
        public override void WriteDecimal128(Decimal128 value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState(nameof(WriteDecimal128), BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.Decimal128Converter.Convert(value, _strictWriter);
            _context.HasElements = true;

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON Double to the writer.
        /// </summary>
        /// <param name="value">The Double value.</param>
        public override void WriteDouble(double value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteDouble", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.DoubleConverter.Convert(value, _strictWriter);
            _context.HasElements = true;

            State = GetNextState();
        }

        /// <summary>
        /// Writes the end of a BSON array to the writer.
        /// </summary>
        public override void WriteEndArray()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteEndArray", BsonWriterState.Value);
            }

            base.WriteEndArray();
            _strictWriter.WriteEndArray();

            _context = _context.ParentContext;
            State = GetNextState();
        }

        /// <summary>
        /// Writes the end of a BSON document to the writer.
        /// </summary>
        public override void WriteEndDocument()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Name)
            {
                ThrowInvalidState("WriteEndDocument", BsonWriterState.Name);
            }

            base.WriteEndDocument();
            _strictWriter.WriteEndDocument();

            if (_context.ContextType == ContextType.ScopeDocument)
            {
                _context = _context.ParentContext;
                WriteEndDocument();
            }
            else
            {
                _context = _context.ParentContext;
            }

            if (_context == null)
            {
                State = BsonWriterState.Done;
            }
            else
            {
                State = GetNextState();
            }
        }

        /// <summary>
        /// Writes a BSON Int32 to the writer.
        /// </summary>
        /// <param name="value">The Int32 value.</param>
        public override void WriteInt32(int value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteInt32", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.Int32Converter.Convert(value, _strictWriter);
            _context.HasElements = true;

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON Int64 to the writer.
        /// </summary>
        /// <param name="value">The Int64 value.</param>
        public override void WriteInt64(long value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteInt64", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.Int64Converter.Convert(value, _strictWriter);
            _context.HasElements = true;

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON JavaScript to the writer.
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
        public override void WriteJavaScript(string code)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteJavaScript", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.JavaScriptConverter.Convert(code, _strictWriter);
            _context.HasElements = true;

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON JavaScript to the writer (call WriteStartDocument to start writing the scope).
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
        public override void WriteJavaScriptWithScope(string code)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteJavaScriptWithScope", BsonWriterState.Value, BsonWriterState.Initial);
            }

            WriteStartDocument();
            WriteName("$code");
            WriteString(code);
            WriteName("$scope");

            State = BsonWriterState.ScopeDocument;
        }

        /// <summary>
        /// Writes a BSON MaxKey to the writer.
        /// </summary>
        public override void WriteMaxKey()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteMaxKey", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.MaxKeyConverter.Convert(BsonMaxKey.Value, _strictWriter);
            _context.HasElements = true;

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON MinKey to the writer.
        /// </summary>
        public override void WriteMinKey()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteMinKey", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.MinKeyConverter.Convert(BsonMinKey.Value, _strictWriter);
            _context.HasElements = true;

            State = GetNextState();
        }

        /// <inheritdoc/>
        public override void WriteName(string name)
        {
            base.WriteName(name);
            _strictWriter.WriteName(name);
        }

        /// <summary>
        /// Writes a BSON null to the writer.
        /// </summary>
        public override void WriteNull()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteNull", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.NullConverter.Convert(BsonNull.Value, _strictWriter);
            _context.HasElements = true;

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON ObjectId to the writer.
        /// </summary>
        /// <param name="objectId">The ObjectId.</param>
        public override void WriteObjectId(ObjectId objectId)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteObjectId", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.ObjectIdConverter.Convert(objectId, _strictWriter);

            _context.HasElements = true;
            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON regular expression to the writer.
        /// </summary>
        /// <param name="regex">A BsonRegularExpression.</param>
        public override void WriteRegularExpression(BsonRegularExpression regex)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteRegularExpression", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.RegularExpressionConverter.Convert(regex, _strictWriter);
            _context.HasElements = true;

            State = GetNextState();
        }

        /// <summary>
        /// Writes the start of a BSON array to the writer.
        /// </summary>
        public override void WriteStartArray()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteStartArray", BsonWriterState.Value, BsonWriterState.Initial);
            }

            base.WriteStartArray();
            _strictWriter.WriteStartArray();
            _context.HasElements = true;

            _context = new JsonWriterContext(_context, ContextType.Array, _jsonWriterSettings.IndentChars);
            State = BsonWriterState.Value;
        }

        /// <summary>
        /// Writes the start of a BSON document to the writer.
        /// </summary>
        public override void WriteStartDocument()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial && State != BsonWriterState.ScopeDocument)
            {
                ThrowInvalidState("WriteStartDocument", BsonWriterState.Value, BsonWriterState.Initial, BsonWriterState.ScopeDocument);
            }

            base.WriteStartDocument();
            _strictWriter.WriteStartDocument();
            _context.HasElements = true;

            var contextType = (State == BsonWriterState.ScopeDocument) ? ContextType.ScopeDocument : ContextType.Document;
            _context = new JsonWriterContext(_context, contextType, _jsonWriterSettings.IndentChars);
            State = BsonWriterState.Name;
        }

        /// <summary>
        /// Writes a BSON String to the writer.
        /// </summary>
        /// <param name="value">The String value.</param>
        public override void WriteString(string value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteString", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.StringConverter.Convert(value, _strictWriter);
            _context.HasElements = true;

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON Symbol to the writer.
        /// </summary>
        /// <param name="value">The symbol.</param>
        public override void WriteSymbol(string value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteSymbol", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.SymbolConverter.Convert(value, _strictWriter);
            _context.HasElements = true;

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON timestamp to the writer.
        /// </summary>
        /// <param name="value">The combined timestamp/increment value.</param>
        public override void WriteTimestamp(long value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteTimestamp", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.TimestampConverter.Convert(value, _strictWriter);
            _context.HasElements = true;

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON undefined to the writer.
        /// </summary>
        public override void WriteUndefined()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteUndefined", BsonWriterState.Value, BsonWriterState.Initial);
            }

            _jsonWriterSettings.Converters.UndefinedConverter.Convert(BsonUndefined.Value, _strictWriter);

            _context.HasElements = true;
            State = GetNextState();
        }

        // protected methods
        /// <summary>
        /// Disposes of any resources used by the writer.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Close();
                }
                catch { } // ignore exceptions
            }
            base.Dispose(disposing);
        }

        // private methods
        private BsonWriterState GetNextState()
        {
            if (_context.ContextType == ContextType.Array || _context.ContextType == ContextType.TopLevel)
            {
                return BsonWriterState.Value;
            }
            else
            {
                return BsonWriterState.Name;
            }
        }
    }
}
