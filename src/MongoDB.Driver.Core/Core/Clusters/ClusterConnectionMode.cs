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

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents the cluster connection mode.
    /// </summary>
    [Obsolete("Use DirectConnection instead.")]
    public enum ClusterConnectionMode
    {
        /// <summary>
        /// Determine the cluster type automatically.
        /// </summary>
        Automatic,

        /// <summary>
        /// Connect directly to a single server of any type.
        /// </summary>
        Direct,

        /// <summary>
        /// Connect directly to a Standalone server.
        /// </summary>
        Standalone,

        /// <summary>
        /// Connect to a replica set.
        /// </summary>
        ReplicaSet,

        /// <summary>
        /// Connect to one or more shard routers.
        /// </summary>
        Sharded
    }

    /// <summary>
    /// Determine whether to use legacy ConnectMode or DirectConnection.
    /// </summary>
    [Obsolete("Will be removed in a later version.")]
    public enum ClusterConnectionModeSwitch
    {
        /// <summary>
        /// The connectionMode is not specified.
        /// </summary>
        NotSet,
        /// <summary>
        /// The legacy connectionMode.
        /// </summary>
        UseConnectionMode,
        /// <summary>
        /// The connectionMode based on directConnection.
        /// </summary>
        UseDirectConnection
    }
}
