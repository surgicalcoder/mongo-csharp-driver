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

using MongoDB.Bson.IO.JsonConverters;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents predefined sets of standard output converters.
    /// </summary>
    public static class JsonOutputConverters
    {
        // private static fields
        private static readonly JsonOutputConverterSet __shell;
        private static readonly JsonOutputConverterSet __strict;

        // static constructor
        static JsonOutputConverters()
        {
            __shell = new JsonOutputConverterSet(
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

            __strict = new JsonOutputConverterSet(
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
        public static JsonOutputConverterSet Shell => __shell;

        /// <summary>
        /// Gets the strict json converters.
        /// </summary>
        public static JsonOutputConverterSet Strict => __strict;
    }
}
