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

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a set of JsonOutputConverters.
    /// </summary>
    public class JsonOutputConverterSet
    {
        // private fields
        private readonly IJsonOutputConverter<BsonBinaryData> _binaryDataConverter;
        private readonly IJsonOutputConverter<bool> _booleanConverter;
        private readonly IJsonOutputConverter<long> _dateTimeConverter;
        private readonly IJsonOutputConverter<Decimal128> _decimal128Converter;
        private readonly IJsonOutputConverter<double> _doubleConverter;
        private readonly IJsonOutputConverter<int> _int32Converter;
        private readonly IJsonOutputConverter<long> _int64Converter;
        private readonly IJsonOutputConverter<string> _javaScriptConverter;
        private readonly IJsonOutputConverter<BsonMaxKey> _maxKeyConverter;
        private readonly IJsonOutputConverter<BsonMinKey> _minKeyConverter;
        private readonly IJsonOutputConverter<BsonNull> _nullConverter;
        private readonly IJsonOutputConverter<ObjectId> _objectIdConverter;
        private readonly IJsonOutputConverter<BsonRegularExpression> _regularExpressionConverter;
        private readonly IJsonOutputConverter<string> _stringConverter;
        private readonly IJsonOutputConverter<string> _symbolConverter;
        private readonly IJsonOutputConverter<long> _timestampConverter;
        private readonly IJsonOutputConverter<BsonUndefined> _undefinedConverter;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonOutputConverterSet"/> class.
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
        public JsonOutputConverterSet(
            IJsonOutputConverter<BsonBinaryData> binaryDataConverter,
            IJsonOutputConverter<bool> booleanConverter,
            IJsonOutputConverter<long> dateTimeConverter,
            IJsonOutputConverter<Decimal128> decimal128Converter,
            IJsonOutputConverter<double> doubleConverter,
            IJsonOutputConverter<int> int32Converter,
            IJsonOutputConverter<long> int64Converter,
            IJsonOutputConverter<string> javaScriptConverter,
            IJsonOutputConverter<BsonMaxKey> maxKeyConverter,
            IJsonOutputConverter<BsonMinKey> minKeyConverter,
            IJsonOutputConverter<BsonNull> nullConverter,
            IJsonOutputConverter<ObjectId> objectIdConverter,
            IJsonOutputConverter<BsonRegularExpression> regularExpressionConverter,
            IJsonOutputConverter<string> stringConverter,
            IJsonOutputConverter<string> symbolConverter,
            IJsonOutputConverter<long> timestampConverter,
            IJsonOutputConverter<BsonUndefined> undefinedConverter)
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
        public IJsonOutputConverter<BsonBinaryData> BinaryDataConverter => _binaryDataConverter;

        /// <summary>
        /// Gets the converter for Boolean values.
        /// </summary>
        public IJsonOutputConverter<bool> BooleanConverter => _booleanConverter;

        /// <summary>
        /// Gets the converter for BsonDateTime values.
        /// </summary>
        public IJsonOutputConverter<long> DateTimeConverter => _dateTimeConverter;

        /// <summary>
        /// Gets the converter for Decimal128 values.
        /// </summary>
        public IJsonOutputConverter<Decimal128> Decimal128Converter => _decimal128Converter;

        /// <summary>
        /// Gets the converter for Double values.
        /// </summary>
        public IJsonOutputConverter<double> DoubleConverter => _doubleConverter;

        /// <summary>
        /// Gets the converter for Int32 values.
        /// </summary>
        public IJsonOutputConverter<int> Int32Converter => _int32Converter;

        /// <summary>
        /// Gets the converter for Int64 values.
        /// </summary>
        public IJsonOutputConverter<long> Int64Converter => _int64Converter;

        /// <summary>
        /// Gets the converter for BsonJavaScript values.
        /// </summary>
        public IJsonOutputConverter<string> JavaScriptConverter => _javaScriptConverter;

        /// <summary>
        /// Gets the converter for BsonMaxKey values.
        /// </summary>
        public IJsonOutputConverter<BsonMaxKey> MaxKeyConverter => _maxKeyConverter;

        /// <summary>
        /// Gets the converter for BsonMinKey values.
        /// </summary>
        public IJsonOutputConverter<BsonMinKey> MinKeyConverter => _minKeyConverter;

        /// <summary>
        /// Gets the converter for BsonNull values.
        /// </summary>
        public IJsonOutputConverter<BsonNull> NullConverter => _nullConverter;

        /// <summary>
        /// Gets the converter for ObjectId values.
        /// </summary>
        public IJsonOutputConverter<ObjectId> ObjectIdConverter => _objectIdConverter;

        /// <summary>
        /// Gets the converter for BsonRegularExpression values.
        /// </summary>
        public IJsonOutputConverter<BsonRegularExpression> RegularExpressionConverter => _regularExpressionConverter;

        /// <summary>
        /// Gets the converter for String values.
        /// </summary>
        public IJsonOutputConverter<string> StringConverter => _stringConverter;

        /// <summary>
        /// Gets the converter for BsonSymbol values.
        /// </summary>
        public IJsonOutputConverter<string> SymbolConverter => _symbolConverter;

        /// <summary>
        /// Gets the converter for BsonTimestamp values.
        /// </summary>
        public IJsonOutputConverter<long> TimestampConverter => _timestampConverter;

        /// <summary>
        /// Gets the converter for BsonUndefined values.
        /// </summary>
        public IJsonOutputConverter<BsonUndefined> UndefinedConverter => _undefinedConverter;

        // public methods
        /// <summary>
        /// Returns a new instance of the <see cref="JsonOutputConverterSet"/> class with some converters replaced.
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
        public JsonOutputConverterSet With(
            IJsonOutputConverter<BsonBinaryData> binaryDataConverter = null,
            IJsonOutputConverter<bool> booleanConverter = null,
            IJsonOutputConverter<long> dateTimeConverter = null,
            IJsonOutputConverter<Decimal128> decimal128Converter = null,
            IJsonOutputConverter<double> doubleConverter = null,
            IJsonOutputConverter<int> int32Converter = null,
            IJsonOutputConverter<long> int64Converter = null,
            IJsonOutputConverter<string> javaScriptConverter = null,
            IJsonOutputConverter<BsonMaxKey> maxKeyConverter = null,
            IJsonOutputConverter<BsonMinKey> minKeyConverter = null,
            IJsonOutputConverter<BsonNull> nullConverter = null,
            IJsonOutputConverter<ObjectId> objectIdConverter = null,
            IJsonOutputConverter<BsonRegularExpression> regularExpressionConverter = null,
            IJsonOutputConverter<string> stringConverter = null,
            IJsonOutputConverter<string> symbolConverter = null,
            IJsonOutputConverter<long> timestampConverter = null,
            IJsonOutputConverter<BsonUndefined> undefinedConverter = null)
        {
            return new JsonOutputConverterSet(
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
