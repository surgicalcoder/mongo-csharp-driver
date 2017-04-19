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

namespace MongoDB.Bson.IO.JsonConverters
{
    /// <summary>
    /// Represents a converter from BsonTimestamp to shell JSON.
    /// </summary>
    public class BsonTimestampShellJsonConverter : IJsonConverter<long>
    {
        /// <inheritdoc/>
        public void Convert(long value, IStrictJsonWriter writer)
        {
            var timestamp = (int)((value >> 32) & 0xffffffff);
            var increment = (int)(value & 0xffffffff);
            var representation = $"Timestamp({JsonConvert.ToString(timestamp)}, {JsonConvert.ToString(increment)})";

            writer.WriteValue(representation);
        }
    }
}
