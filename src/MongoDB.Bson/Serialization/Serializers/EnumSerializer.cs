/* Copyright 2010-present MongoDB Inc.
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
using System.IO;
using System.Linq;
using System.Reflection;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for enums.
    /// </summary>
    /// <typeparam name="TEnum">The type of the enum.</typeparam>
    public class EnumSerializer<TEnum> : StructSerializerBase<TEnum>, IRepresentationConfigurable<EnumSerializer<TEnum>> where TEnum : struct
    {
        // private fields
        private readonly BsonType _representation;
        private readonly TypeCode _underlyingTypeCode;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumSerializer{TEnum}"/> class.
        /// </summary>
        public EnumSerializer()
            : this((BsonType)0) // 0 means use underlying type
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumSerializer{TEnum}"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        public EnumSerializer(BsonType representation)
        {
            switch (representation)
            {
                case 0:
                case BsonType.Int32:
                case BsonType.Int64:
                case BsonType.String:
                    break;

                default:
                    var message = string.Format("{0} is not a valid representation for an EnumSerializer.", representation);
                    throw new ArgumentException(message);
            }

            // don't know of a way to enforce this at compile time
            var enumTypeInfo = typeof(TEnum).GetTypeInfo();
            if (!enumTypeInfo.IsEnum)
            {
                var message = string.Format("{0} is not an enum type.", typeof(TEnum).FullName);
                throw new BsonSerializationException(message);
            }

            _representation = representation;
            _underlyingTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(TEnum)));
        }

        // public properties
        /// <summary>
        /// Gets the representation.
        /// </summary>
        /// <value>
        /// The representation.
        /// </value>
        public BsonType Representation
        {
            get { return _representation; }
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <param name="args">The deserialization args.</param>
        /// <returns>A deserialized value.</returns>
        public override TEnum Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Int32: return ConvertInt32ToEnum(bsonReader.ReadInt32());
                case BsonType.Int64: return ConvertInt64ToEnum(bsonReader.ReadInt64());
                case BsonType.Double: return ConvertDoubleToEnum(bsonReader.ReadDouble());
                case BsonType.String: return ConvertStringToEnum(bsonReader.ReadString());
                default:
                    throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TEnum value)
        {
            var bsonWriter = context.Writer;

            switch (_representation)
            {
                case 0:
                    if (_underlyingTypeCode == TypeCode.Int64 || _underlyingTypeCode == TypeCode.UInt64)
                    {
                        goto case BsonType.Int64;
                    }
                    else
                    {
                        goto case BsonType.Int32;
                    }

                case BsonType.Int32:
                    bsonWriter.WriteInt32(ConvertEnumToInt32(value));
                    break;

                case BsonType.Int64:
                    bsonWriter.WriteInt64(ConvertEnumToInt64(value));
                    break;

                case BsonType.String:
                    bsonWriter.WriteString(ConvertEnumToString(value));
                    break;

                default:
                    throw new BsonInternalException("Unexpected EnumRepresentation.");
            }
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified representation.
        /// </summary>
        /// <param name="representation">The representation.</param>
        /// <returns>The reconfigured serializer.</returns>
        public EnumSerializer<TEnum> WithRepresentation(BsonType representation)
        {
            if (representation == _representation)
            {
                return this;
            }
            else
            {
                return new EnumSerializer<TEnum>(representation);
            }
        }

        // explicit interface implementations
        IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }

        private TEnum ConvertDoubleToEnum(double value)
        {
            return ConvertInt64ToEnum(checked((long)value));
        }

        private int ConvertEnumToInt32(TEnum value)
        {
            var representationConverter = new RepresentationConverter(allowOverflow: false, allowTruncation: false);
            switch (_underlyingTypeCode)
            {
                case TypeCode.Byte: return (int)(byte)(object)value;
                case TypeCode.Int16: return (int)(short)(object)value;
                case TypeCode.Int32: return (int)(object)value;
                case TypeCode.Int64: return checked((int)(long)(object)value);
                case TypeCode.SByte: return (int)(sbyte)(object)value;
                case TypeCode.UInt16: return (int)(ushort)(object)value;
                case TypeCode.UInt32: return (int)(uint)(object)value;
                case TypeCode.UInt64: return (int)(checked((uint)(ulong)(object)value));
                default: throw new InvalidOperationException($"Unexpected underlying type code: {_underlyingTypeCode}.");
            }
        }

        private long ConvertEnumToInt64(TEnum value)
        {
            switch (_underlyingTypeCode)
            {
                case TypeCode.Byte: return (long)(byte)(object)value;
                case TypeCode.Int16: return (long)(short)(object)value;
                case TypeCode.Int32: return (long)(int)(object)value;
                case TypeCode.Int64: return (long)(object)value;
                case TypeCode.SByte: return (long)(sbyte)(object)value;
                case TypeCode.UInt16: return (long)(ushort)(object)value;
                case TypeCode.UInt32: return (long)(uint)(object)value;
                case TypeCode.UInt64: return (long)(ulong)(object)value;
                default: throw new InvalidOperationException($"Unexpected underlying type code: {_underlyingTypeCode}.");
            }
        }

        private string ConvertEnumToString(TEnum value)
        {
            return value.ToString();
        }

        private TEnum ConvertInt64ToEnum(long value)
        {
            switch (_underlyingTypeCode)
            {
                case TypeCode.Byte: return (TEnum)Enum.ToObject(typeof(TEnum), checked((byte)value));
                case TypeCode.Int16: return (TEnum)Enum.ToObject(typeof(TEnum), checked((short)value));
                case TypeCode.Int32: return (TEnum)Enum.ToObject(typeof(TEnum), checked((int)value));
                case TypeCode.Int64: return (TEnum)Enum.ToObject(typeof(TEnum), value);
                case TypeCode.SByte: return (TEnum)Enum.ToObject(typeof(TEnum), checked((sbyte)value));
                case TypeCode.UInt16: return (TEnum)Enum.ToObject(typeof(TEnum), checked((ushort)value));
                case TypeCode.UInt32: return (TEnum)Enum.ToObject(typeof(TEnum), checked((uint)value));
                case TypeCode.UInt64: return (TEnum)Enum.ToObject(typeof(TEnum), (ulong)value);
                default: throw new InvalidOperationException($"Unexpected underlying type code: {_underlyingTypeCode}.");
            }
        }

        private TEnum ConvertInt32ToEnum(int value)
        {
            switch (_underlyingTypeCode)
            {
                case TypeCode.Byte: return (TEnum)Enum.ToObject(typeof(TEnum), checked((byte)value));
                case TypeCode.Int16: return (TEnum)Enum.ToObject(typeof(TEnum), checked((short)value));
                case TypeCode.Int32: return (TEnum)Enum.ToObject(typeof(TEnum), value);
                case TypeCode.Int64: return (TEnum)Enum.ToObject(typeof(TEnum), (long)value);
                case TypeCode.SByte: return (TEnum)Enum.ToObject(typeof(TEnum), checked((sbyte)value));
                case TypeCode.UInt16: return (TEnum)Enum.ToObject(typeof(TEnum), checked((ushort)value));
                case TypeCode.UInt32: return (TEnum)Enum.ToObject(typeof(TEnum), checked((uint)value));
                case TypeCode.UInt64: return (TEnum)Enum.ToObject(typeof(TEnum), (ulong)value);
                default: throw new InvalidOperationException($"Unexpected underlying type code: {_underlyingTypeCode}.");
            }
        }

        private TEnum ConvertStringToEnum(string value)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), value);
        }
    }
}
