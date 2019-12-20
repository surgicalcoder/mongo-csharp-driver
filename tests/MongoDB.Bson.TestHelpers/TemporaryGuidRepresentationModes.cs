/* Copyright 2019-present MongoDB Inc.
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

using System.Collections.Generic;

namespace MongoDB.Bson.TestHelpers
{
    public static class TemporaryGuidRepresentationModes
    {
        private static readonly IEnumerable<TemporaryGuidRepresentationMode> __all;
        private static readonly TemporaryGuidRepresentationMode __v2CSharpLegacy = new TemporaryGuidRepresentationMode(GuidRepresentationMode.V2, GuidRepresentation.CSharpLegacy);
        private static readonly TemporaryGuidRepresentationMode __v2JavaLegacy = new TemporaryGuidRepresentationMode(GuidRepresentationMode.V2, GuidRepresentation.JavaLegacy);
        private static readonly TemporaryGuidRepresentationMode __v2PytonLegacy = new TemporaryGuidRepresentationMode(GuidRepresentationMode.V2, GuidRepresentation.PythonLegacy);
        private static readonly TemporaryGuidRepresentationMode __v2Standard = new TemporaryGuidRepresentationMode(GuidRepresentationMode.V2, GuidRepresentation.Standard);
        private static readonly TemporaryGuidRepresentationMode __v2Unspecified = new TemporaryGuidRepresentationMode(GuidRepresentationMode.V2, GuidRepresentation.Unspecified);
        private static readonly TemporaryGuidRepresentationMode __v3 = new TemporaryGuidRepresentationMode(GuidRepresentationMode.V3);

        static TemporaryGuidRepresentationModes()
        {
            __all = new[]
            {
                __v2CSharpLegacy,
                __v2JavaLegacy,
                __v2PytonLegacy,
                __v2Standard,
                __v2Unspecified,
                __v3,
            };
        }

        public static IEnumerable<TemporaryGuidRepresentationMode> All => __all;
        public static TemporaryGuidRepresentationMode V2CSharpLegacy => __v2CSharpLegacy;
        public static TemporaryGuidRepresentationMode V2JavaLegacy => __v2JavaLegacy;
        public static TemporaryGuidRepresentationMode V2PythonLegacy => __v2PytonLegacy;
        public static TemporaryGuidRepresentationMode V2Standard => __v2Standard;
        public static TemporaryGuidRepresentationMode V2Unspecified => __v2Unspecified;
        public static TemporaryGuidRepresentationMode V3 => __v3;
    }
}
