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
using MongoDB.Bson.IO.JsonConverters;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a set of JsonConverters.
    /// </summary>
    public class JsonConverterSet
    {
        #region static
        // private static fields
        private static readonly JsonConverterSet __shell;
        private static readonly JsonConverterSet __strict;

        // static constructor
        static JsonConverterSet()
        {
            __shell = new JsonConverterSet(
                new BsonBinaryDataShellJsonConverter(),
                new BooleanStrictJsonConverter(),
                new BsonDateTimeShellJsonConverter(),
                new Decimal128ShellJsonConverter(),
                new DoubleWithDecimalPointJsonConverter(),
                new Int32StrictJsonConverter(),
                new Int64ShellJsonConverter(),
                new BsonJavaScriptExtendedJsonConverter(),
                new BsonMaxKeyShellJsonConverter(),
                new BsonMinKeyShellJsonConverter(),
                new BsonNullStrictJsonConverter(),
                new ObjectIdShellJsonConverter(),
                new BsonRegularExpressionShellJsonConverter(),
                new StringStrictJsonConverter(),
                new BsonSymbolExtendedJsonConverter(),
                new BsonTimestampShellJsonConverter(),
                new BsonUndefinedShellJsonConverter());

            __strict = new JsonConverterSet(
                new BsonBinaryDataExtendedJsonConverter(),
                new BooleanStrictJsonConverter(),
                new BsonDateTimeExtendedJsonConverter(),
                new Decimal128ExtendedJsonConverter(),
                new DoubleWithDecimalPointJsonConverter(),
                new Int32StrictJsonConverter(),
                new Int64StrictJsonConverter(),
                new BsonJavaScriptExtendedJsonConverter(),
                new BsonMaxKeyExtendedJsonConverter(),
                new BsonMinKeyExtendedJsonConverter(),
                new BsonNullStrictJsonConverter(),
                new ObjectIdExtendedJsonConverter(),
                new BsonRegularExpressionExtendedJsonConverter(),
                new StringStrictJsonConverter(),
                new BsonSymbolExtendedJsonConverter(),
                new BsonTimestampExtendedJsonConverter(),
                new BsonUndefinedExtendedJsonConverter());
        }

        // public static properties
        /// <summary>
        /// Gets the shell json converters.
        /// </summary>
        public static JsonConverterSet Shell => __shell;

        /// <summary>
        /// Gets the strict json converters.
        /// </summary>
        public static JsonConverterSet Strict => __strict;
        #endregion

        // private fields
        private readonly IJsonConverter<BsonBinaryData> _binaryDataConverter;
        private readonly IJsonConverter<bool> _booleanConverter;
        private readonly IJsonConverter<long> _dateTimeConverter;
        private readonly IJsonConverter<Decimal128> _decimal128Converter;
        private readonly IJsonConverter<double> _doubleConverter;
        private readonly IJsonConverter<int> _int32Converter;
        private readonly IJsonConverter<long> _int64Converter;
        private readonly IJsonConverter<string> _javaScriptConverter;
        private readonly IJsonConverter<BsonMaxKey> _maxKeyConverter;
        private readonly IJsonConverter<BsonMinKey> _minKeyConverter;
        private readonly IJsonConverter<BsonNull> _nullConverter;
        private readonly IJsonConverter<ObjectId> _objectIdConverter;
        private readonly IJsonConverter<BsonRegularExpression> _regularExpressionConverter;
        private readonly IJsonConverter<string> _stringConverter;
        private readonly IJsonConverter<string> _symbolConverter;
        private readonly IJsonConverter<long> _timestampConverter;
        private readonly IJsonConverter<BsonUndefined> _undefinedConverter;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonConverterSet"/> class.
        /// </summary>
        /// <param name="binaryDataConverter">The BsonBinaryData converter.</param>
        /// <param name="booleanConverter">The Boolean converter.</param>
        /// <param name="dateTimeConverter">The BsonDateTime converter.</param>
        /// <param name="decimal128Converter">The Decimal128 converter.</param>
        /// <param name="doubleConverter">The Double converter.</param>
        /// <param name="int32Converter">The Int32 converter.</param>
        /// <param name="int64Converter">The Int64 converter.</param>
        /// <param name="javaScriptConverter">The BsonJavaScript converter.</param>
        /// <param name="maxKeyConverter">The BsonMaxKey converter.</param>
        /// <param name="minKeyConverter">The BsonMinKey converter.</param>
        /// <param name="nullConverter">The BsonNull converter.</param>
        /// <param name="objectIdConverter">The ObjectId converter.</param>
        /// <param name="regularExpressionConverter">The BsonRegularExpression converter.</param>
        /// <param name="stringConverter">The String converter.</param>
        /// <param name="symbolConverter">The BsonSymbol converter.</param>
        /// <param name="timestampConverter">The BsonTimestamp converter.</param>
        /// <param name="undefinedConverter">The BsonUndefined converter.</param>
        public JsonConverterSet(
            IJsonConverter<BsonBinaryData> binaryDataConverter,
            IJsonConverter<bool> booleanConverter,
            IJsonConverter<long> dateTimeConverter,
            IJsonConverter<Decimal128> decimal128Converter,
            IJsonConverter<double> doubleConverter,
            IJsonConverter<int> int32Converter,
            IJsonConverter<long> int64Converter,
            IJsonConverter<string> javaScriptConverter,
            IJsonConverter<BsonMaxKey> maxKeyConverter,
            IJsonConverter<BsonMinKey> minKeyConverter,
            IJsonConverter<BsonNull> nullConverter,
            IJsonConverter<ObjectId> objectIdConverter,
            IJsonConverter<BsonRegularExpression> regularExpressionConverter,
            IJsonConverter<string> stringConverter,
            IJsonConverter<string> symbolConverter,
            IJsonConverter<long> timestampConverter,
            IJsonConverter<BsonUndefined> undefinedConverter)
        {
            if (binaryDataConverter == null) { throw new ArgumentNullException(nameof(binaryDataConverter)); }
            if (booleanConverter == null) { throw new ArgumentNullException(nameof(booleanConverter)); }
            if (dateTimeConverter == null) { throw new ArgumentNullException(nameof(dateTimeConverter)); }
            if (decimal128Converter == null) { throw new ArgumentNullException(nameof(decimal128Converter)); }
            if (doubleConverter == null) { throw new ArgumentNullException(nameof(doubleConverter)); }
            if (int32Converter == null) { throw new ArgumentNullException(nameof(int32Converter)); }
            if (int64Converter == null) { throw new ArgumentNullException(nameof(int64Converter)); }
            if (javaScriptConverter == null) { throw new ArgumentNullException(nameof(javaScriptConverter)); }
            if (maxKeyConverter == null) { throw new ArgumentNullException(nameof(maxKeyConverter)); }
            if (minKeyConverter == null) { throw new ArgumentNullException(nameof(minKeyConverter)); }
            if (nullConverter == null) { throw new ArgumentNullException(nameof(nullConverter)); }
            if (objectIdConverter == null) { throw new ArgumentNullException(nameof(objectIdConverter)); }
            if (regularExpressionConverter == null) { throw new ArgumentNullException(nameof(regularExpressionConverter)); }
            if (stringConverter == null) { throw new ArgumentNullException(nameof(stringConverter)); }
            if (symbolConverter == null) { throw new ArgumentNullException(nameof(symbolConverter)); }
            if (timestampConverter == null) { throw new ArgumentNullException(nameof(timestampConverter)); }
            if (undefinedConverter == null) { throw new ArgumentNullException(nameof(undefinedConverter)); }

            _binaryDataConverter = binaryDataConverter;
            _booleanConverter = booleanConverter;
            _dateTimeConverter = dateTimeConverter;
            _decimal128Converter = decimal128Converter;
            _doubleConverter = doubleConverter;
            _int32Converter = int32Converter;
            _int64Converter = int64Converter;
            _javaScriptConverter = javaScriptConverter;
            _maxKeyConverter = maxKeyConverter;
            _minKeyConverter = minKeyConverter;
            _nullConverter = nullConverter;
            _objectIdConverter = objectIdConverter;
            _regularExpressionConverter = regularExpressionConverter;
            _stringConverter = stringConverter;
            _symbolConverter = symbolConverter;
            _timestampConverter = timestampConverter;
            _undefinedConverter = undefinedConverter;
        }

        // public properties
        /// <summary>
        /// Gets the converter for BsonBinaryData values.
        /// </summary>
        public IJsonConverter<BsonBinaryData> BinaryDataConverter => _binaryDataConverter;

        /// <summary>
        /// Gets the converter for Boolean values.
        /// </summary>
        public IJsonConverter<bool> BooleanConverter => _booleanConverter;

        /// <summary>
        /// Gets the converter for BsonDateTime values.
        /// </summary>
        public IJsonConverter<long> DateTimeConverter => _dateTimeConverter;

        /// <summary>
        /// Gets the converter for Decimal128 values.
        /// </summary>
        public IJsonConverter<Decimal128> Decimal128Converter => _decimal128Converter;

        /// <summary>
        /// Gets the converter for Double values.
        /// </summary>
        public IJsonConverter<double> DoubleConverter => _doubleConverter;

        /// <summary>
        /// Gets the converter for Int32 values.
        /// </summary>
        public IJsonConverter<int> Int32Converter => _int32Converter;

        /// <summary>
        /// Gets the converter for Int64 values.
        /// </summary>
        public IJsonConverter<long> Int64Converter => _int64Converter;

        /// <summary>
        /// Gets the converter for BsonJavaScript values.
        /// </summary>
        public IJsonConverter<string> JavaScriptConverter => _javaScriptConverter;

        /// <summary>
        /// Gets the converter for BsonMaxKey values.
        /// </summary>
        public IJsonConverter<BsonMaxKey> MaxKeyConverter => _maxKeyConverter;

        /// <summary>
        /// Gets the converter for BsonMinKey values.
        /// </summary>
        public IJsonConverter<BsonMinKey> MinKeyConverter => _minKeyConverter;

        /// <summary>
        /// Gets the converter for BsonNull values.
        /// </summary>
        public IJsonConverter<BsonNull> NullConverter => _nullConverter;

        /// <summary>
        /// Gets the converter for ObjectId values.
        /// </summary>
        public IJsonConverter<ObjectId> ObjectIdConverter => _objectIdConverter;

        /// <summary>
        /// Gets the converter for BsonRegularExpression values.
        /// </summary>
        public IJsonConverter<BsonRegularExpression> RegularExpressionConverter => _regularExpressionConverter;

        /// <summary>
        /// Gets the converter for String values.
        /// </summary>
        public IJsonConverter<string> StringConverter => _stringConverter;

        /// <summary>
        /// Gets the converter for BsonSymbol values.
        /// </summary>
        public IJsonConverter<string> SymbolConverter => _symbolConverter;

        /// <summary>
        /// Gets the converter for BsonTimestamp values.
        /// </summary>
        public IJsonConverter<long> TimestampConverter => _timestampConverter;

        /// <summary>
        /// Gets the converter for BsonUndefined values.
        /// </summary>
        public IJsonConverter<BsonUndefined> UndefinedConverter => _undefinedConverter;

        // public methods
        /// <summary>
        /// Returns a new instance of the <see cref="JsonConverterSet"/> class with some converters replaced.
        /// </summary>
        /// <param name="binaryDataConverter">The BsonBinaryData converter.</param>
        /// <param name="booleanConverter">The Boolean converter.</param>
        /// <param name="dateTimeConverter">The BsonDateTime converter.</param>
        /// <param name="decimal128Converter">The Decimal128 converter.</param>
        /// <param name="doubleConverter">The Double converter.</param>
        /// <param name="int32Converter">The Int32 converter.</param>
        /// <param name="int64Converter">The Int64 converter.</param>
        /// <param name="javaScriptConverter">The BsonJavaScript converter.</param>
        /// <param name="maxKeyConverter">The BsonMaxKey converter.</param>
        /// <param name="minKeyConverter">The BsonMinKey converter.</param>
        /// <param name="nullConverter">The BsonNull converter.</param>
        /// <param name="objectIdConverter">The ObjectId converter.</param>
        /// <param name="regularExpressionConverter">The BsonRegularExpression converter.</param>
        /// <param name="stringConverter">The String converter.</param>
        /// <param name="symbolConverter">The BsonSymbol converter.</param>
        /// <param name="timestampConverter">The BsonTimestamp converter.</param>
        /// <param name="undefinedConverter">The BsonUndefined converter.</param>
        public JsonConverterSet With(
            IJsonConverter<BsonBinaryData> binaryDataConverter = null,
            IJsonConverter<bool> booleanConverter = null,
            IJsonConverter<long> dateTimeConverter = null,
            IJsonConverter<Decimal128> decimal128Converter = null,
            IJsonConverter<double> doubleConverter = null,
            IJsonConverter<int> int32Converter = null,
            IJsonConverter<long> int64Converter = null,
            IJsonConverter<string> javaScriptConverter = null,
            IJsonConverter<BsonMaxKey> maxKeyConverter = null,
            IJsonConverter<BsonMinKey> minKeyConverter = null,
            IJsonConverter<BsonNull> nullConverter = null,
            IJsonConverter<ObjectId> objectIdConverter = null,
            IJsonConverter<BsonRegularExpression> regularExpressionConverter = null,
            IJsonConverter<string> stringConverter = null,
            IJsonConverter<string> symbolConverter = null,
            IJsonConverter<long> timestampConverter = null,
            IJsonConverter<BsonUndefined> undefinedConverter = null)
        {
            return new JsonConverterSet(
                binaryDataConverter ?? _binaryDataConverter,
                booleanConverter ?? _booleanConverter,
                dateTimeConverter ?? _dateTimeConverter,
                decimal128Converter ?? _decimal128Converter,
                doubleConverter ?? _doubleConverter,
                int32Converter ?? _int32Converter,
                int64Converter ?? _int64Converter,
                javaScriptConverter ?? _javaScriptConverter,
                maxKeyConverter ?? _maxKeyConverter,
                minKeyConverter ?? _minKeyConverter,
                nullConverter ?? _nullConverter,
                objectIdConverter ?? _objectIdConverter,
                regularExpressionConverter ?? _regularExpressionConverter,
                stringConverter ?? _stringConverter,
                symbolConverter ?? _symbolConverter,
                timestampConverter ?? _timestampConverter,
                undefinedConverter ?? _undefinedConverter);
        }
    }
}
