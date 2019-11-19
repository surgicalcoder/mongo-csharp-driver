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

using System;

namespace MongoDB.Bson.TestHelpers
{
    public static class TemporaryGuidRepresentationMode

    {
#pragma warning disable 618
        public static IDisposable V3()
        {
            var resetter = new Resetter(BsonDefaults.GuidRepresentationMode);
            BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
            return resetter;
        }

        private class Resetter : IDisposable
        {
            private GuidRepresentationMode _originalMode;

            public Resetter(GuidRepresentationMode originalMode)
            {
                _originalMode = originalMode;
            }

            public void Dispose()
            {
                BsonDefaults.GuidRepresentationMode = _originalMode;
            }
        }
#pragma warning restore 618
    }
}
