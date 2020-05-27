using System;
using MongoDB.Bson;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Servers
{
    /// <inheritdoc/>
    public sealed class TopologyVersion : IEquatable<TopologyVersion>, IConvertibleToBsonDocument
    {
        #region static
        // public static methods
        /// <inheritdoc/>
        public static int CompareTopologyVersion(TopologyVersion x, TopologyVersion y)
        {
            if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
            {
                return -1;
            }

            return x.CompareTopologyVersion(y);
        }

        /// <inheritdoc/>
        public static bool Equals(TopologyVersion x, TopologyVersion y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
            {
                return false;
            }

            return x.Equals(y);
        }

        /// <inheritdoc/>
        public static TopologyVersion FromBsonDocument(BsonDocument document)
        {
            if (document.TryGetValue("processId", out var processIdValue) && processIdValue is BsonObjectId processId &&
                document.TryGetValue("counter", out var counterValue) && counterValue is BsonInt64 counter)
            {
                return new TopologyVersion(processId.Value, counter.Value);
            }

            return null;
        }

        internal static TopologyVersion FromMongoCommandResponse(BsonDocument response)
        {
            if (response != null &&
                response.TryGetValue("topologyVersion", out var topologyVersionValue) && topologyVersionValue is BsonDocument topologyVersion)
            {
                return FromBsonDocument(topologyVersion);
            }

            return null;
        }

        internal static TopologyVersion FromMongoCommandException(MongoCommandException commandException)
        {
            return FromMongoCommandResponse(commandException.Result);
        }

        // public static operators
        /// <inheritdoc/>
        public static bool operator ==(TopologyVersion x, TopologyVersion y) => Equals(x, y);

        /// <inheritdoc/>
        public static bool operator !=(TopologyVersion x, TopologyVersion y) => !Equals(x, y);
        #endregion

        // private fields
        private readonly long _counter;
        private readonly int _hashCode;
        private readonly ObjectId _processId;

        // constructors
        /// <inheritdoc/>
        public TopologyVersion(ObjectId processId, long counter)
        {
            _processId = processId;
            _counter = counter;
            _hashCode = new Hasher().Hash(_processId).Hash(_counter).GetHashCode();
        }

        // public properties
        /// <inheritdoc/>
        public long Counter => _counter;

        /// <inheritdoc/>
        public ObjectId ProcessId => _processId;

        // public methods
        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as TopologyVersion);
        }

        /// <inheritdoc />
        public bool Equals(TopologyVersion other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            return _counter == other._counter && _processId == other._processId;
        }

        /// <inheritdoc/>
        public int CompareTopologyVersion(TopologyVersion other)
        {
            if (other == null)
            {
                return -1;
            }

            if (_processId == other.ProcessId)
            {
                return _counter.CompareTo(other.Counter);
            }

            return -1;
        }

        /// <inheritdoc/>
        public bool IsFresherThan(TopologyVersion other) => CompareTopologyVersion(other) > 0;

        /// <inheritdoc/>
        public bool IsFresherThanOrEqualTo(TopologyVersion other) => CompareTopologyVersion(other) >= 0;

        /// <inheritdoc/>
        public bool IsStalerThanOrEqualTo(TopologyVersion other) => CompareTopologyVersion(other) <= 0;

        /// <inheritdoc/>
        public override int GetHashCode() => _hashCode;

        /// <inheritdoc/>
        public BsonDocument ToBsonDocument() => new BsonDocument { { "processId", _processId }, { "counter", _counter } };

        /// <inheritdoc/>
        public override string ToString() => ToBsonDocument().ToJson();
    }
}
