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

namespace MongoDB.Bson.IO.JsonConverters
{
    /// <summary>
    /// Represents a converter between BsonBinaryData values and shell JSON.
    /// </summary>
    public class BsonBinaryDataShellJsonConverter : IJsonOutputConverter<BsonBinaryData>
    {
        // public methods
        /// <inheritdoc/>
        public void Write(IStrictJsonWriter writer, BsonBinaryData value)
        {
            string representation;
            switch (value.SubType)
            {
                case BsonBinarySubType.UuidLegacy:
                case BsonBinarySubType.UuidStandard:
                    representation = GuidToString(value.SubType, value.Bytes, value.GuidRepresentation);
                    break;

                default:
                    var subType = JsonConvert.ToString((int)value.SubType);
                    var base64Bytes = System.Convert.ToBase64String(value.Bytes);
                    representation = $"new BinData({subType}, \"{base64Bytes}\")";
                    break;
            }

            writer.WriteValue(representation);
        }

        // private methods
        private string GuidToString(BsonBinarySubType subType, byte[] bytes, GuidRepresentation guidRepresentation)
        {
            if (bytes.Length != 16)
            {
                var message = string.Format("Length of binary subtype {0} must be 16, not {1}.", subType, bytes.Length);
                throw new ArgumentException(message);
            }
            if (subType == BsonBinarySubType.UuidLegacy && guidRepresentation == GuidRepresentation.Standard)
            {
                throw new ArgumentException("GuidRepresentation for binary subtype UuidLegacy must not be Standard.");
            }
            if (subType == BsonBinarySubType.UuidStandard && guidRepresentation != GuidRepresentation.Standard)
            {
                var message = string.Format("GuidRepresentation for binary subtype UuidStandard must be Standard, not {0}.", guidRepresentation);
                throw new ArgumentException(message);
            }

            if (guidRepresentation == GuidRepresentation.Unspecified)
            {
                var s = BsonUtils.ToHexString(bytes);
                var parts = new string[]
                {
                    s.Substring(0, 8),
                    s.Substring(8, 4),
                    s.Substring(12, 4),
                    s.Substring(16, 4),
                    s.Substring(20, 12)
                };
                return string.Format("HexData({0}, \"{1}\")", (int)subType, string.Join("-", parts));
            }
            else
            {
                string uuidConstructorName;
                switch (guidRepresentation)
                {
                    case GuidRepresentation.CSharpLegacy: uuidConstructorName = "CSUUID"; break;
                    case GuidRepresentation.JavaLegacy: uuidConstructorName = "JUUID"; break;
                    case GuidRepresentation.PythonLegacy: uuidConstructorName = "PYUUID"; break;
                    case GuidRepresentation.Standard: uuidConstructorName = "UUID"; break;
                    default: throw new BsonInternalException("Unexpected GuidRepresentation");
                }
                var guid = GuidConverter.FromBytes(bytes, guidRepresentation);
                return string.Format("{0}(\"{1}\")", uuidConstructorName, guid.ToString());
            }
        }
    }
}
