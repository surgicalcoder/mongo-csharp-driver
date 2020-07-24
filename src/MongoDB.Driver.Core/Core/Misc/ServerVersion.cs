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
using System.Text;
using System.Text.RegularExpressions;

namespace MongoDB.Driver.Core.Misc
{
    /// <summary>
    /// Represents a server version number.
    /// </summary>
    public class ServerVersion : IEquatable<ServerVersion>, IComparable<ServerVersion>
    {
        #region static
        private static bool TryParseInternalPrelease(string preReleaseIn, out string preReleaseOut, out int? commitsAfterRelease, out string commitHash)
        {
            if (preReleaseIn != null)
            {
                var internalBuildPattern = @"^(?<preRelease>.+-)?(?<commitsAfterRelease>\d+)-g(?<commitHash>[0-9a-fA-F]{4,40})$";
                var match = Regex.Match(preReleaseIn, internalBuildPattern);
                if (match.Success)
                {
                    var preReleaseGroup = match.Groups["preRelease"];
                    preReleaseOut = preReleaseGroup.Success ? preReleaseGroup.Value : null;
                    commitsAfterRelease = int.Parse(match.Groups["commitsAfterRelease"].Value);
                    commitHash = match.Groups["commitHash"].Value;
                    return true;
                }
            }

            preReleaseOut = preReleaseIn;
            commitsAfterRelease = null;
            commitHash = null;
            return false;
        }
        #endregion

        // fields
        private readonly string _commitHash;
        private readonly int? _commitsAfterRelease;
        private readonly int _major;
        private readonly int _minor;
        private readonly int _patch;
        private readonly string _preRelease;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerVersion"/> class.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        /// <param name="patch">The patch version.</param>
        public ServerVersion(int major, int minor, int patch)
            : this(major, minor, patch, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerVersion"/> class.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        /// <param name="patch">The patch version.</param>
        /// <param name="preRelease">The pre release version.</param>
        public ServerVersion(int major, int minor, int patch, string preRelease)
        {
            _major = Ensure.IsGreaterThanOrEqualToZero(major, nameof(major));
            _minor = Ensure.IsGreaterThanOrEqualToZero(minor, nameof(minor));
            _patch = Ensure.IsGreaterThanOrEqualToZero(patch, nameof(patch));

            if (TryParseInternalPrelease(preRelease, out preRelease, out var commitsAfterRelease, out var commitHash))
            {
                _preRelease = preRelease; // can be null
                _commitsAfterRelease = commitsAfterRelease;
                _commitHash = commitHash;
            }
            else
            {
                _preRelease = preRelease; // can be null
                _commitsAfterRelease = null;
                _commitHash = null;
            }
        }

        // properties
        /// <summary>
        /// 
        /// </summary>
        public int? CommitsAfterRelease => _commitsAfterRelease;

        /// <summary>
        ///
        /// </summary>
        public string CommitHash => _commitHash;

        /// <summary>
        /// Gets the major version.
        /// </summary>
        /// <value>
        /// The major version.
        /// </value>
        public int Major
        {
            get { return _major; }
        }

        /// <summary>
        /// Gets the minor version.
        /// </summary>
        /// <value>
        /// The minor version.
        /// </value>
        public int Minor
        {
            get { return _minor; }
        }

        /// <summary>
        /// Gets the patch version.
        /// </summary>
        /// <value>
        /// The patch version.
        /// </value>
        public int Patch
        {
            get { return _patch; }
        }

        /// <summary>
        /// Gets the pre release version.
        /// </summary>
        /// <value>
        /// The pre release version.
        /// </value>
        public string PreRelease
        {
            get { return _preRelease; }
        }

        // methods
        /// <inheritdoc/>
        public int CompareTo(ServerVersion other)
        {
            if (other == null)
            {
                return 1;
            }

            var result = _major.CompareTo(other._major);
            if (result != 0)
            {
                return result;
            }

            result = _minor.CompareTo(other._minor);
            if (result != 0)
            {
                return result;
            }

            result = _patch.CompareTo(other._patch);
            if (result != 0)
            {
                return result;
            }

            result = ComparePreReleases(_preRelease, other._preRelease);
            if (result != 0)
            {
                return result;
            }

            result = CompareCommitsAfterRelease(_commitsAfterRelease, other._commitsAfterRelease);
            if (result != 0)
            {
                return result;
            }

            // ignore _commitHash for comparison purposes
            return 0;

            int ComparePreReleases(string x, string y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }
                else if (x == null)
                {
                    return 1;
                }
                else if (y == null)
                {
                    return -1;
                }

                return x.CompareTo(y);
            }

            int CompareCommitsAfterRelease(int? x, int? y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }
                else if (x == null)
                {
                    return -1;
                }
                else if (y == null)
                {
                    return 1;
                }

                return x.Value.CompareTo(y.Value);
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ServerVersion);
        }

        /// <inheritdoc/>
        public bool Equals(ServerVersion other)
        {
            return CompareTo(other) == 0;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(_major);
            sb.Append('.');
            sb.Append(_minor);
            sb.Append('.');
            sb.Append(_patch);
            if (_preRelease != null)
            {
                sb.Append('-');
                sb.Append(_preRelease);
            }
            if (_commitsAfterRelease != null)
            {
                sb.Append('-');
                sb.Append(_commitsAfterRelease);
            }
            if (_commitHash != null)
            {
                sb.Append("-g");
                sb.Append(_commitHash);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parses a string representation of a server version.
        /// </summary>
        /// <param name="value">The string value to parse.</param>
        /// <returns>A server version.</returns>
        public static ServerVersion Parse(string value)
        {
            ServerVersion result;
            if (TryParse(value, out result))
            {
                return result;
            }

            throw new FormatException(string.Format(
                "Invalid ServerVersion string: '{0}'.", value));
        }

        /// <summary>
        /// Tries to parse a string representation of a server version.
        /// </summary>
        /// <param name="value">The string value to parse.</param>
        /// <param name="result">The result.</param>
        /// <returns>True if the string representation was parsed successfully; otherwise false.</returns>
        public static bool TryParse(string value, out ServerVersion result)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var pattern = @"(?<major>\d+)\.(?<minor>\d+)(\.(?<patch>\d+)(-(?<preRelease>.*))?)?";
                var match = Regex.Match((string)value, pattern);
                if (match.Success)
                {
                    var major = int.Parse(match.Groups["major"].Value);
                    var minor = int.Parse(match.Groups["minor"].Value);
                    var patch = match.Groups["patch"].Success ? int.Parse(match.Groups["patch"].Value) : 0;
                    var preRelease = match.Groups["preRelease"].Success ? match.Groups["preRelease"].Value : null;

                    result = new ServerVersion(major, minor, patch, preRelease);
                    return true;
                }
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Determines whether two specified server versions have the same value.
        /// </summary>
        /// <param name="a">The first server version to compare, or null.</param>
        /// <param name="b">The second server version to compare, or null.</param>
        /// <returns>
        /// True if the value of a is the same as the value of b; otherwise false.
        /// </returns>
        public static bool operator ==(ServerVersion a, ServerVersion b)
        {
            if (object.ReferenceEquals(a, null))
            {
                return object.ReferenceEquals(b, null);
            }

            return a.CompareTo(b) == 0;
        }

        /// <summary>
        /// Determines whether two specified server versions have different values.
        /// </summary>
        /// <param name="a">The first server version to compare, or null.</param>
        /// <param name="b">The second server version to compare, or null.</param>
        /// <returns>
        /// True if the value of a is different from the value of b; otherwise false.
        /// </returns>
        public static bool operator !=(ServerVersion a, ServerVersion b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Determines whether the first specified ServerVersion is greater than the second specified ServerVersion.
        /// </summary>
        /// <param name="a">The first server version to compare, or null.</param>
        /// <param name="b">The second server version to compare, or null.</param>
        /// <returns>
        /// True if the value of a is greater than b; otherwise false.
        /// </returns>
        public static bool operator >(ServerVersion a, ServerVersion b)
        {
            if (a == null)
            {
                if (b == null)
                {
                    return true;
                }

                return false;
            }

            return a.CompareTo(b) > 0;
        }

        /// <summary>
        /// Determines whether the first specified ServerVersion is greater than or equal to the second specified ServerVersion.
        /// </summary>
        /// <param name="a">The first server version to compare, or null.</param>
        /// <param name="b">The second server version to compare, or null.</param>
        /// <returns>
        /// True if the value of a is greater than or equal to b; otherwise false.
        /// </returns>
        public static bool operator >=(ServerVersion a, ServerVersion b)
        {
            return !(a < b);
        }

        /// <summary>
        /// Determines whether the first specified ServerVersion is less than the second specified ServerVersion.
        /// </summary>
        /// <param name="a">The first server version to compare, or null.</param>
        /// <param name="b">The second server version to compare, or null.</param>
        /// <returns>
        /// True if the value of a is less than b; otherwise false.
        /// </returns>
        public static bool operator <(ServerVersion a, ServerVersion b)
        {
            return b > a;
        }

        /// <summary>
        /// Determines whether the first specified ServerVersion is less than or equal to the second specified ServerVersion.
        /// </summary>
        /// <param name="a">The first server version to compare, or null.</param>
        /// <param name="b">The second server version to compare, or null.</param>
        /// <returns>
        /// True if the value of a is less than or equal to b; otherwise false.
        /// </returns>
        public static bool operator <=(ServerVersion a, ServerVersion b)
        {
            return !(b < a);
        }
    }
}
