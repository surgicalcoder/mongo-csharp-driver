/* Copyright 2013-present MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents information about a cluster.
    /// </summary>
    public sealed class ClusterDescription : IEquatable<ClusterDescription>
    {
        #region static
        // internal static methods
#pragma warning disable CS0618
        internal static ClusterDescription CreateInitial(ClusterId clusterId, bool? directConnection)
#pragma warning restore CS0618
        {
            return new ClusterDescription(
                clusterId,
                directConnection,
                ClusterType.Unknown,
                Enumerable.Empty<ServerDescription>());
        }

#pragma warning disable CS0618
        [Obsolete("Use the overload with DirectConnection isntead.")]
        internal static ClusterDescription CreateInitial(ClusterId clusterId, ConnectionModeSwitch connectionModeSwitch, ClusterConnectionMode connectionMode)
#pragma warning restore CS0618
        {
            return new ClusterDescription(
                clusterId,
                connectionMode,
                ClusterType.Unknown,
                Enumerable.Empty<ServerDescription>(),
                connectionModeSwitch);
        }

        // private static methods
        private static TimeSpan? CalculateLogicalSessionTimeout(ClusterDescription clusterDescription, IEnumerable<ServerDescription> servers)
        {
            TimeSpan? logicalSessionTimeout = null;

            foreach (var server in SelectServersThatDetermineWhetherSessionsAreSupported(clusterDescription, servers))
            {
                if (server.LogicalSessionTimeout == null)
                {
                    return null;
                }

                if (logicalSessionTimeout == null || server.LogicalSessionTimeout.Value < logicalSessionTimeout.Value)
                {
                    logicalSessionTimeout = server.LogicalSessionTimeout;
                }
            }

            return logicalSessionTimeout;
        }

        private static IEnumerable<ServerDescription> SelectServersThatDetermineWhetherSessionsAreSupported(ClusterDescription clusterDescription, IEnumerable<ServerDescription> servers)
        {
            var connectedServers = servers.Where(s => s.State == ServerState.Connected);
            if (IsDirectConnection())
            {
                return connectedServers;
            }
            else
            {
                return connectedServers.Where(s => s.IsDataBearing);
            }

            bool IsDirectConnection()
            {
#pragma warning disable CS0618
                if (clusterDescription.ConnectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
                {
                    return clusterDescription.DirectConnection.GetValueOrDefault();
                }
                else
                {
                    return clusterDescription.ConnectionMode == ClusterConnectionMode.Direct;
                }
#pragma warning restore CS0618
            }
        }
        #endregion

        // fields
        private readonly ClusterId _clusterId;
#pragma warning disable CS0618
        private readonly ClusterConnectionMode _connectionMode;
        private readonly ConnectionModeSwitch _connectionModeSwitch;
#pragma warning restore CS0618
        private readonly bool? _directConnection;
        private readonly Exception _dnsMonitorException;
        private readonly TimeSpan? _logicalSessionTimeout;
        private readonly IReadOnlyList<ServerDescription> _servers;
        private readonly ClusterType _type;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterDescription" /> class.
        /// </summary>
        /// <param name="clusterId">The cluster identifier.</param>
        /// <param name="connectionMode">The connection mode.</param>
        /// <param name="type">The type.</param>
        /// <param name="servers">The servers.</param>
        /// <param name="connectionModeSwitch">The connectionMode switch.</param>
        [Obsolete("Use the overload with DirectConnection.")]
        public ClusterDescription(
            ClusterId clusterId,
            ClusterConnectionMode connectionMode,
            ClusterType type,
            IEnumerable<ServerDescription> servers,
            ConnectionModeSwitch connectionModeSwitch = ConnectionModeSwitch.NotSet)
            : this(
                  clusterId,
                  connectionMode,
                  dnsMonitorException: null,
                  type,
                  servers,
                  connectionModeSwitch)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterDescription" /> class.
        /// </summary>
        /// <param name="clusterId">The cluster identifier.</param>
        /// <param name="directConnection">The directConnection.</param>
        /// <param name="type">The type.</param>
        /// <param name="servers">The servers.</param>
        public ClusterDescription(
            ClusterId clusterId,
            bool? directConnection,
            ClusterType type,
            IEnumerable<ServerDescription> servers)
            : this(
                  clusterId,
                  directConnection,
                  dnsMonitorException: null,
                  type,
                  servers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterDescription" /> class.
        /// </summary>
        /// <param name="clusterId">The cluster identifier.</param>
        /// <param name="connectionMode">The connection mode.</param>
        /// <param name="dnsMonitorException">The last DNS monitor exception (null if there was none).</param>
        /// <param name="type">The type.</param>
        /// <param name="servers">The servers.</param>
        /// <param name="connectionModeSwitch">The connectionMode switch.</param>
        [Obsolete("Use the overload with DirectConnection.")]
        public ClusterDescription(
            ClusterId clusterId,
#pragma warning disable CS0618
            ClusterConnectionMode connectionMode,
#pragma warning restore CS0618
            Exception dnsMonitorException,
            ClusterType type,
            IEnumerable<ServerDescription> servers,
#pragma warning disable CS0618
            ConnectionModeSwitch connectionModeSwitch = ConnectionModeSwitch.NotSet)
            : this(
                  clusterId: clusterId,
                  connectionMode,
                  directConnection: null,
                  dnsMonitorException,
                  type,
                  servers,
                  connectionModeSwitch)
#pragma warning restore CS0618
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterDescription" /> class.
        /// </summary>
        /// <param name="clusterId">The cluster identifier.</param>
        /// <param name="directConnection">The directConnection.</param>
        /// <param name="dnsMonitorException">The last DNS monitor exception (null if there was none).</param>
        /// <param name="type">The type.</param>
        /// <param name="servers">The servers.</param>
        public ClusterDescription(
            ClusterId clusterId,
            bool? directConnection,
            Exception dnsMonitorException,
            ClusterType type,
            IEnumerable<ServerDescription> servers)
#pragma warning disable CS0618
            : this(
                  clusterId: clusterId,
                  connectionMode: ClusterConnectionMode.Automatic,
                  directConnection: directConnection,
                  dnsMonitorException: dnsMonitorException,
                  type: type,
                  servers: servers,
                  ConnectionModeSwitch.UseDirectConnection)
#pragma warning restore CS0618
        {
        }

        private ClusterDescription(ClusterId clusterId,
#pragma warning disable CS0618
            ClusterConnectionMode connectionMode,
#pragma warning restore CS0618
            bool? directConnection,
            Exception dnsMonitorException,
            ClusterType type,
            IEnumerable<ServerDescription> servers,
#pragma warning disable CS0618
            ConnectionModeSwitch connectionModeSwitch)
#pragma warning restore CS0618
        {
            _clusterId = Ensure.IsNotNull(clusterId, nameof(clusterId));
            _connectionModeSwitch = connectionModeSwitch;
#pragma warning disable CS0618
            if (_connectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
#pragma warning restore CS0618
            {
                _directConnection = directConnection; // can be null
            }
            else
            {
                _connectionMode = connectionMode;
            }
            _dnsMonitorException = dnsMonitorException; // can be null
            _type = type;
            _servers = (servers ?? new ServerDescription[0]).OrderBy(n => n.EndPoint, new ToStringComparer<EndPoint>()).ToList();
            _logicalSessionTimeout = CalculateLogicalSessionTimeout(this, _servers);
        }

        // properties
        /// <summary>
        /// Gets the cluster identifier.
        /// </summary>
        public ClusterId ClusterId
        {
            get { return _clusterId; }
        }

        /// <summary>
        /// Gets the connection mode.
        /// </summary>
        [Obsolete("Use DirectConnection instead.")]
        public ClusterConnectionMode ConnectionMode
        {
            get
            {
                if (_connectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
                {
                    throw new InvalidOperationException("ConnectionMode cannot be used when ConnectionModeSwitch is set to UseDirectConnection.");
                }
                return _connectionMode;
            }
        }

        /// <summary>
        /// Gets the connectionMode switch.
        /// </summary>
        [Obsolete("Will be removed in a later version.")]
        public ConnectionModeSwitch ConnectionModeSwitch
        {
            get { return _connectionModeSwitch; }
        }

        /// <summary>
        /// Gets the DirectConnection.
        /// </summary>
        public bool? DirectConnection
        {
            get
            {
#pragma warning disable CS0618
                if (_connectionModeSwitch == ConnectionModeSwitch.UseConnectionMode)
#pragma warning restore CS0618
                {
                    throw new InvalidOperationException("DirectConnection cannot be used when ConnectionModeSwitch is set to UseConnectionMode.");
                }
                return _directConnection;
            }
        }

        /// <summary>
        /// Gets the last DNS monitor exception (null if there was none).
        /// </summary>
        public Exception DnsMonitorException
        {
            get { return _dnsMonitorException; }
        }

        /// <summary>
        /// Gets a value indicating whether this cluster is compatible with the driver.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this cluster is compatible with the driver; otherwise, <c>false</c>.
        /// </value>
        public bool IsCompatibleWithDriver
        {
            get
            {
                return _servers.All(s => s.IsCompatibleWithDriver);
            }
        }

        /// <summary>
        /// Gets the logical session timeout.
        /// </summary>
        public TimeSpan? LogicalSessionTimeout
        {
            get { return _logicalSessionTimeout; }
        }

        /// <summary>
        /// Gets the servers.
        /// </summary>
        public IReadOnlyList<ServerDescription> Servers
        {
            get { return _servers; }
        }

        /// <summary>
        /// Gets the cluster state.
        /// </summary>
        public ClusterState State
        {
            get { return _servers.Any(x => x.State == ServerState.Connected) ? ClusterState.Connected : ClusterState.Disconnected; }
        }

        /// <summary>
        /// Gets the cluster type.
        /// </summary>
        public ClusterType Type
        {
            get { return _type; }
        }

        // methods
        /// <inheritdoc/>
        public bool Equals(ClusterDescription other)
        {
            if (other == null)
            {
                return false;
            }

            return
                _clusterId.Equals(other._clusterId) &&
                _connectionMode == other._connectionMode &&
                object.Equals(_directConnection, other._directConnection) &&
                object.Equals(_dnsMonitorException, other._dnsMonitorException) &&
                _servers.SequenceEqual(other._servers) &&
                _type == other._type;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ClusterDescription);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            // ignore _revision
            return new Hasher()
                .Hash(_clusterId)
                .Hash(_connectionMode)
                .Hash(_directConnection)
                .Hash(_dnsMonitorException)
                .HashElements(_servers)
                .Hash(_type)
                .GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var servers = string.Join(", ", _servers.Select(n => n.ToString()).ToArray());
            var value = string.Format(
                "{{ ClusterId : \"{0}\", {1}Type : \"{2}\", State : \"{3}\", Servers : [{4}] }}",
                _clusterId,
                GetConnectionMode(),
                _type,
                State,
                servers);
            if (_dnsMonitorException != null)
            {
                value = value.Substring(0, value.Length - 2) + string.Format(", DnsMonitorException : \"{0}\" }}", _dnsMonitorException);
            }
            return value;

            string GetConnectionMode()
            {
#pragma warning disable CS0618
                if (_connectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
                {
                    return _directConnection.HasValue ? $"DirectConnection : \"{_directConnection}\", " : string.Empty;
                }
                else if (_connectionModeSwitch == ConnectionModeSwitch.UseConnectionMode)
#pragma warning restore CS0618
                {
                    return $"ConnectionMode : \"{_connectionMode}\", ";
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Returns a new ClusterDescription with a changed DnsMonitorException.
        /// </summary>
        /// <param name="value">The exception.</param>
        /// <returns>A ClusterDescription.</returns>
        public ClusterDescription WithDnsMonitorException(Exception value)
        {
            if (value != _dnsMonitorException)
            {
                return new ClusterDescription(
                    _clusterId,
                    _connectionMode,
                    _directConnection,
                    value,
                    _type,
                    _servers,
                    _connectionModeSwitch);
            }

            return this;
        }

        /// <summary>
        /// Returns a new ClusterDescription with a changed ServerDescription.
        /// </summary>
        /// <param name="value">The server description.</param>
        /// <returns>A ClusterDescription.</returns>
        public ClusterDescription WithServerDescription(ServerDescription value)
        {
            Ensure.IsNotNull(value, nameof(value));

            IEnumerable<ServerDescription> replacementServers;

            var oldServerDescription = _servers.SingleOrDefault(s => s.EndPoint == value.EndPoint);
            if (oldServerDescription != null)
            {
                if (oldServerDescription.Equals(value))
                {
                    return this;
                }

                replacementServers = _servers.Select(s => s.EndPoint == value.EndPoint ? value : s);
            }
            else
            {
                replacementServers = _servers.Concat(new[] { value });
            }

            return new ClusterDescription(
                _clusterId,
                _connectionMode,
                _directConnection,
                _dnsMonitorException,
                _type,
                replacementServers,
                _connectionModeSwitch);
        }

        /// <summary>
        /// Returns a new ClusterDescription with a ServerDescription removed.
        /// </summary>
        /// <param name="endPoint">The end point of the server description to remove.</param>
        /// <returns>A ClusterDescription.</returns>
        public ClusterDescription WithoutServerDescription(EndPoint endPoint)
        {
            var oldServerDescription = _servers.SingleOrDefault(s => s.EndPoint == endPoint);
            if (oldServerDescription == null)
            {
                return this;
            }

            return new ClusterDescription(
                _clusterId,
                _connectionMode,
                _directConnection,
                _dnsMonitorException,
                _type,
                _servers.Where(s => !EndPointHelper.Equals(s.EndPoint, endPoint)),
                _connectionModeSwitch);
        }

        /// <summary>
        /// Returns a new ClusterDescription with a changed ClusterType.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A ClusterDescription.</returns>
        public ClusterDescription WithType(ClusterType value)
        {
            return _type == value ? this : new ClusterDescription(_clusterId, _connectionMode, _directConnection, _dnsMonitorException, value, _servers, _connectionModeSwitch);
        }
    }
}
