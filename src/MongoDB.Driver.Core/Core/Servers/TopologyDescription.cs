/* Copyright 2020–present MongoDB Inc.
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
using MongoDB.Shared;
#if NET452 || NETSTANDARD2_0
using System.Runtime.Serialization;
#endif

namespace MongoDB.Driver.Core.Servers
{
    /// <summary>
    /// Represents a topology description.
    /// Comparing topology descriptions freshness does not exhibit the reversal property of
    /// inequalities e.g. a.IsStalerThan(b) (a "&lt;" b) does not imply !b.IsStalerThan(a) (b "&gt;" a)
    /// See <seealso cref="CompareFreshnessToServerResponse"/> for more information.
    /// </summary>
#if NET452 || NETSTANDARD2_0
    [Serializable]
    public readonly struct TopologyDescription : IEquatable<TopologyDescription>, ISerializable
#else
    public readonly struct TopologyDescription : IEquatable<TopologyDescription>
#endif
    {
        // fields
        private readonly ObjectId _processId;
        private readonly long _counter;
        private readonly int _hashCode;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TopologyDescription"/> class.
        /// </summary>
        /// <param name="processId">The process identifier.</param>
        /// <param name="counter">The counter.</param>
        public TopologyDescription(ObjectId processId, long counter)
        {
            _processId = processId;
            _counter = counter;
            _hashCode = new Hasher().Hash(_processId).Hash(_counter).GetHashCode();
        }

#if NET452 || NETSTANDARD2_0
        private TopologyDescription(SerializationInfo info, StreamingContext context)
        {
            _processId = (ObjectId) info.GetValue("_processId", typeof(ObjectId));
            _counter = (long) info.GetValue("_counter", typeof(long));
            _hashCode = new Hasher().Hash(_processId).Hash(_counter).GetHashCode();
        }
#endif

        // properties
        /// <summary>
        /// Gets the process identifier.
        /// </summary>
        /// <value>
        /// The process identifier.
        /// </value>
        public ObjectId ProcessId => _processId;

        /// <summary>
        /// Gets the counter.
        /// </summary>
        /// <value>
        /// The counter.
        /// </value>
        public long Counter => _counter;

        // methods
        /// <summary>
        /// Gets whether <paramref name="x"/>.Equals(<paramref name="y"/>).
        /// </summary>
        /// <param name="x">A TopologyDescription.</param>
        /// <param name="y">A TopologyDescription.</param>
        /// <returns>
        /// Whether <paramref name="x"/>.Equals(<paramref name="y"/>).
        /// </returns>
        public static bool operator ==(TopologyDescription x, TopologyDescription y) => CompareFreshnessOfLocalToServerResponse(x, y) == 0;

        /// <summary>
        /// Gets whether <paramref name="x"/>  != (<paramref name="y"/>).
        /// </summary>
        /// <param name="x">A TopologyDescription.</param>
        /// <param name="y">A TopologyDescription.</param>
        /// <returns>
        /// Whether <paramref name="x"/> != (<paramref name="y"/>).
        /// </returns>
        public static bool operator !=(TopologyDescription x, TopologyDescription y) => CompareFreshnessOfLocalToServerResponse(x, y) != 0;

        /// <summary>
        /// Compares a local TopologyDescription with aserver's TopologyDescription and indicates whether the local
        /// TopologyDescription is staler, fresher, or equal to the server's TopologyDescription.
        /// Per the SDAM specification, if the ProcessIds are not equal, this method assumes that
        /// <paramref name="serverResponse"/> is more recent. This means that this method does not exhibit
        /// the reversal properties of inequalities i.e. a "&lt;" b does not imply b "&gt;" a.
        /// </summary>
        /// <param name="local"> The locally saved TopologyDescription. </param>
        /// <param name="serverResponse">The TopologyDescription received from a server.</param>
        /// <returns>
        /// Less than zero indicates that the local description is older than the response.
        /// Zero indicates that the local description is equal to the response.
        /// Greater than zero indicates that the local description is newer than the response.
        /// </returns>
        public static int CompareFreshnessOfLocalToServerResponse(TopologyDescription local, TopologyDescription serverResponse)
        {
            // Per the spec, if the ProcessIds are not equal, this method assumes that serverResponse is more recent
            // See https://github.com/mongodb/specifications/blob/master/source/server-discovery-and-monitoring/server-discovery-and-monitoring.rst#topologyversion-comparison
            return local.ProcessId == serverResponse.ProcessId ? local.Counter.CompareTo(serverResponse.Counter) : -1;
        }

        /// <summary>
        /// Attempts to create a TopologyDescription from the supplied BsonDocument.
        /// </summary>
        /// <param name="topologyVersion">The document. Should contain an ObjectId named "processId" and a BsonInt64 named "counter".</param>
        /// <returns>A TopologyDescription if one could be constructed from the supplied document and null otherwise. </returns>
        public static TopologyDescription? FromBsonDocument(BsonDocument topologyVersion)
        {
            return
                topologyVersion.Contains("processId") &&
                topologyVersion["processId"] is BsonObjectId processId &&
                topologyVersion.Contains("counter") &&
                topologyVersion["counter"] is BsonInt64 counter
                ? new TopologyDescription(processId.Value, counter.Value)
                : (TopologyDescription?)null;
        }

        internal static TopologyDescription? FromMongoCommandResponse(BsonDocument response)
        {
            return
                response != null &&
                response.TryGetValue("topologyVersion", out var topologyVersionValue) &&
                topologyVersionValue is BsonDocument topologyVersion
                ? TopologyDescription.FromBsonDocument(topologyVersion)
                : null;
        }

        internal static TopologyDescription? FromMongoCommandException(MongoCommandException commandException)
        {
            if (FromMongoCommandResponse(commandException.Result) is TopologyDescription responseTopologyVersion)
            {
                return responseTopologyVersion;
            }

            return null;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return (obj is TopologyDescription) && (CompareFreshnessOfLocalToServerResponse(this, (TopologyDescription)obj) == 0);
        }

        /// <inheritdoc />
        public bool Equals(TopologyDescription other) => CompareFreshnessOfLocalToServerResponse(this, other) == 0;

        /// <summary>
        /// Compares this TopologyDescription with another TopologyDescription and indicates whether this
        /// TopologyDescription is precedes, follows, or appears in the same position in the sort order.
        /// The sort order will order TopologyDescriptions from oldest to most recent.
        /// Per the SDAM specification, if the ProcessIds are not equal, this method assumes that
        /// <paramref name="serverResponse"/> is more recent. This means that this method does not exhibit
        /// the reversal properties of inequalities i.e. a "&lt;" b does not imply b "&gt;" a.
        /// </summary>
        /// <param name="serverResponse">The TopologyDescription received from a server.</param>
        /// <returns>
        /// Less than zero indicates that this description is older than the response.
        /// Zero indicates that this description is equal to the response.
        /// Greater than zero indicates that this description is newer than the response.
        /// </returns>
        public int CompareFreshnessToServerResponse(TopologyDescription serverResponse) => CompareFreshnessOfLocalToServerResponse(this, serverResponse);

        /// <summary>
        /// Gets whether or not this TopologyDescription is fresher than <paramref name="serverResponse"/>.
        /// Comparing topology descriptions freshness does not exhibit the reversal property of
        /// inequalities e.g. a.IsStalerThanServerResponse(b) (a "&lt;" b) does not imply
        /// !b.IsStalerThanServerResponse(a) (b "&gt;" a)
        /// See <seealso cref="CompareFreshnessToServerResponse"/> for more information.
        /// In the case that this == <paramref name="serverResponse"/>,
        /// <paramref name="serverResponse"/> will be consider to be fresher.
        /// </summary>
        /// <param name="serverResponse">The TopologyDescription received from a server/ </param>
        /// <returns>
        /// Wwhether or not this TopologyDescription is fresher than <paramref name="serverResponse"/>.
        /// </returns>
        public bool IsFresherThanServerResponse(TopologyDescription serverResponse) => CompareFreshnessToServerResponse(serverResponse) > 0;

        /// <summary>
        /// Gets whether or not this TopologyDescription is fresher than <paramref name="serverResponse"/>.
        /// Comparing topology descriptions freshness does not exhibit the reversal property of
        /// inequalities e.g. a.IsStalerThanServerResponse(b) (a "&lt;" b) does not imply
        /// !b.IsStalerThanServerResponse(a) (b "&gt;" a)
        /// See <seealso cref="CompareFreshnessToServerResponse"/> for more information.
        /// In the case that this == <paramref name="serverResponse"/>,
        /// <paramref name="serverResponse"/> will be consider to be fresher.
        /// </summary>
        /// <param name="serverResponse">The TopologyDescription received from a server/ </param>
        /// <returns>
        /// Wwhether or not this TopologyDescription is fresher than <paramref name="serverResponse"/>.
        /// </returns>
        public bool IsFresherThanOrEqualToServerResponse(TopologyDescription serverResponse) => CompareFreshnessToServerResponse(serverResponse) >= 0;

        /// <summary>
        /// Gets whether or not this TopologyDescription is staler than <paramref name="serverResponse"/>.
        /// Comparing topology descriptions freshness does not exhibit the reversal property of
        /// inequalities e.g. a.IsStalerThanServerResponse(b) (a "&lt;" b) does not imply
        /// !b.IsStalerThanServerResponse(a) (b "&gt;" a).
        /// See <seealso cref="CompareFreshnessToServerResponse"/> for more information.
        /// In the case that this == <paramref name="serverResponse"/>,
        /// <paramref name="serverResponse"/> will be consider to be fresher.
        /// </summary>
        /// <param name="serverResponse">The TopologyDescription received from a server/ </param>
        /// <returns>
        /// Wwhether or not this TopologyDescription is staler than <paramref name="serverResponse"/>.
        /// </returns>
        public bool IsStalerThanServerResponse(TopologyDescription serverResponse) => CompareFreshnessToServerResponse(serverResponse) <= 0;

        /// <inheritdoc/>
        public override int GetHashCode() => _hashCode;

        /// <inheritdoc/>
        public override string ToString() => $"{{ ProcessId : {_processId}, Counter : '{_counter}' }}";

        // explicit interface implementations
#if NET452 || NETSTANDARD2_0
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(_processId), _processId, typeof(ObjectId));
            info.AddValue(nameof(_counter), _counter, typeof(long));
        }
#endif
    }

    /// <summary>
    /// Extensions for Nullable TopologyDescription
    /// </summary>
    public static class NullableTopologyDescriptionExtensions
    {
        /// <summary>
        /// Compares a local TopologyDescription with a server's TopologyDescription and indicates whether the local
        /// TopologyDescription is staler, fresher, or equal to the server's TopologyDescription.
        /// Per the SDAM specification, if the ProcessIds are not equal, this method assumes that
        /// <paramref name="serverResponse"/> is more recent. This means that this method does not exhibit
        /// the reversal properties of inequalities e.g. a &lt; b does not imply b &gt; a.
        /// </summary>
        /// <param name="local"> The locally saved TopologyDescription. </param>
        /// <param name="serverResponse">The TopologyDescription received from a server.</param>
        /// <returns>
        /// Less than zero indicates that the local description is older than the response.
        /// Zero indicates that the local description is equal to the response.
        /// Greater than zero indicates that the local description is newer than the response.
        /// </returns>
        public static int CompareFreshnessToServerResponse(this TopologyDescription? local, TopologyDescription? serverResponse)
        {
            return (local == null || serverResponse == null) ? -1 : local.Value.CompareFreshnessToServerResponse(serverResponse.Value);
        }

        /// <summary>
        /// Gets whether or not <paramref name="local"/> is staler than <paramref name="serverResponse"/>.
        /// Comparing topology descriptions freshness does not exhibit the reversal property of
        /// inequalities e.g. a.IsStalerThanServerResponse(b) (a "&lt;" b) does not imply
        /// !b.IsStalerThanServerResponse(a) (b "&gt;" a)
        /// In the case that <paramref name="local"/> == <paramref name="serverResponse"/>,
        /// <paramref name="serverResponse"/> will be consider to be fresher.
        /// </summary>
        /// <param name="local"> The locally saved TopologyDescription. </param>
        /// <param name="serverResponse">The TopologyDescription received from a server.</param>
        /// <returns>
        /// Whether or not <paramref name="local"/> is staler than <paramref name="serverResponse"/>.
        /// </returns>
        public static bool IsFresherThanServerResponse(this TopologyDescription? local, TopologyDescription? serverResponse)
        {
            return serverResponse != null && local != null && local.Value.IsFresherThanServerResponse(serverResponse.Value);
        }

        /// <summary>
        /// Gets whether or not <paramref name="local"/> is staler than <paramref name="serverResponse"/>.
        /// Comparing topology descriptions freshness does not exhibit the reversal property of
        /// inequalities e.g. a.IsStalerThanServerResponse(b) (a "&lt;" b) does not imply
        /// !b.IsStalerThanServerResponse(a) (b "&gt;" a)
        /// In the case that <paramref name="local"/> == <paramref name="serverResponse"/>,
        /// <paramref name="serverResponse"/> will be consider to be fresher.
        /// </summary>
        /// <param name="local"> The locally saved TopologyDescription. </param>
        /// <param name="serverResponse">The TopologyDescription received from a server.</param>
        /// <returns>
        /// Whether or not <paramref name="local"/> is staler than <paramref name="serverResponse"/>.
        /// </returns>
        public static bool IsFresherThanOrEqualToServerResponse(this TopologyDescription? local, TopologyDescription? serverResponse)
        {
            return serverResponse != null && local != null && local.Value.IsFresherThanOrEqualToServerResponse(serverResponse.Value);
        }
        /// <summary>
        /// Gets whether or not <paramref name="local"/> is staler than <paramref name="serverResponse"/>.
        /// Comparing topology descriptions freshness does not exhibit the reversal property of
        /// inequalities e.g. a.IsStalerThanServerResponse(b) (a "&lt;" b) does not imply
        /// !b.IsStalerThanServerResponse(a) (b "&gt;" a).
        /// In the case that <paramref name="local"/> == <paramref name="serverResponse"/>,
        /// <paramref name="serverResponse"/> will be consider to be fresher.
        /// </summary>
        /// <param name="local"> The locally saved TopologyDescription. </param>
        /// <param name="serverResponse">The TopologyDescription received from a server.</param>
        /// <returns>
        /// Whether or not <paramref name="local"/> is staler than <paramref name="serverResponse"/>.
        /// </returns>
        public static bool IsStalerThanServerResponse(this TopologyDescription? local, TopologyDescription? serverResponse)
        {
            return serverResponse == null || local == null || local.Value.IsStalerThanServerResponse(serverResponse.Value);
        }
    }
}
