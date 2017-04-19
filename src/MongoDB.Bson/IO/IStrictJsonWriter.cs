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

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a writer for strict JSON.
    /// </summary>
    public interface IStrictJsonWriter
    {
        /// <summary>
        /// Writes a Boolean value.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteBoolean(bool value);

        /// <summary>
        /// Writes a Double value.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteDouble(double value);

        /// <summary>
        /// Ends an array.
        /// </summary>
        void WriteEndArray();

        /// <summary>
        /// Ends a document.
        /// </summary>
        void WriteEndDocument();

        /// <summary>
        /// Writes an Int32 value.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteInt32(int value);

        /// <summary>
        /// Writes an Int64 value.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteInt64(long value);

        /// <summary>
        /// Writes a name.
        /// </summary>
        /// <param name="name">The name.</param>
        void WriteName(string name);

        /// <summary>
        /// Writes a null.
        /// </summary>
        void WriteNull();

        /// <summary>
        /// Starts an array.
        /// </summary>
        void WriteStartArray();

        /// <summary>
        /// Starts a document.
        /// </summary>
        void WriteStartDocument();

        /// <summary>
        /// Writes a String value.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteString(string value);

        /// <summary>
        /// Writes a value.
        /// </summary>
        /// <param name="representation">The representation.</param>
        void WriteValue(string representation);
    }
}
