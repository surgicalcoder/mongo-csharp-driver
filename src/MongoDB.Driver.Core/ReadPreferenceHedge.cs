/* Copyright 2020-present MongoDB Inc.
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
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the read preference hedge.
    /// </summary>
    public abstract class ReadPreferenceHedge : IEquatable<ReadPreferenceHedge>
    {
        #region static
        // private static fields
        private static readonly CustomReadPreferenceHedge __enabled = new CustomReadPreferenceHedge(isEnabled: true);
        private static readonly ReadPreferenceHedge __serverDefault = new ServerDefaultReadPreferenceHedge();

        // public static properties
        /// <summary>
        /// Gets an enabled read preference hedge.
        /// </summary>
        public static ReadPreferenceHedge Enabled => __enabled;

        /// <summary>
        /// Gets a server default read preference hedge.
        /// </summary>
        public static ReadPreferenceHedge ServerDefault => __serverDefault;
        #endregion

        // public methods
        /// <inheritdoc/>
        public abstract bool Equals(ReadPreferenceHedge other);

        /// <summary>
        /// Returns the BsonDocument representation of a hedge.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public abstract BsonDocument ToBsonDocument();

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToBsonDocument().ToJson();
        }
    }

    /// <summary>
    /// Represents a custom read preference hedge.
    /// </summary>
    public sealed class CustomReadPreferenceHedge : ReadPreferenceHedge
    {
        // private fields
        private readonly bool _isEnabled;

        // constructors
        /// <summary>
        /// Initializes an instance of CustomReadPreferenceHedge.
        /// </summary>
        /// <param name="isEnabled">Whether hedged reads are enabled.</param>
        public CustomReadPreferenceHedge(bool isEnabled)
        {
            _isEnabled = isEnabled;
        }

        // public properties
        /// <summary>
        /// Gets whether hedged reads are enabled.
        /// </summary>
        public bool IsEnabled => _isEnabled;
        
        // public methods
        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            return Equals(other as CustomReadPreferenceHedge);
        }

        /// <inheritdoc/>
        public override bool Equals(ReadPreferenceHedge other)
        {
            return
                other is CustomReadPreferenceHedge customOther &&
                _isEnabled == customOther._isEnabled;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _isEnabled.GetHashCode();
        }

        /// <inheritdoc/>
        public override BsonDocument ToBsonDocument()
        {
            return new BsonDocument("enabled", _isEnabled);
        }
    }

    /// <summary>
    /// Represents the server default read preference hedge.
    /// </summary>
    public sealed class ServerDefaultReadPreferenceHedge : ReadPreferenceHedge
    {
        // public methods
        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            return Equals(other as ServerDefaultReadPreferenceHedge);
        }

        /// <inheritdoc/>
        public override bool Equals(ReadPreferenceHedge other)
        {
            return other is ServerDefaultReadPreferenceHedge;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return 0;
        }

        /// <inheritdoc/>
        public override BsonDocument ToBsonDocument()
        {
            return new BsonDocument();
        }
    }
}
