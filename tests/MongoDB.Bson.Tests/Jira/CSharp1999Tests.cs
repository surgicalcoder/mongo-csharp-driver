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

using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class C
    {
        private readonly List<int> _l;

        public C()
        {
            _l = new List<int>();
        }

        [BsonElement]
        public List<int> L => _l;
    }

    public class CSharp1999Tests
    {
        [Fact]
        public void Deserialize_should_return_expected_result()
        {
            var c = new C();
            c.L.AddRange(new[] { 1, 2, 3 });

            var json = c.ToJson();

            var r = BsonSerializer.Deserialize<C>(json);
        }
    }
}
