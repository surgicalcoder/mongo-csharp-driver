/* Copyright 2020⁠–⁠present MongoDB Inc.
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
using System.Net;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Xunit;


namespace MongoDB.Driver.Core.Servers
{
    public class TopologyVersionTests
    {
        [Fact]
        public void Constructor_should_properly_initialize_instance()
        {
            var processId = ObjectId.Empty;
            var counter = 42L;

            var subject = new TopologyVersion(processId, counter);

            subject.ProcessId.Should().Be(processId);
            subject.Counter.Should().Be(counter);
        }

        [Fact]
        public void CompareFreshnessToServerResponse_should_always_return_negative_if_comparand_is_null()
        {
            var processId = ObjectId.Empty;
            var counter = 31L;

            TopologyVersion nullResponse = null;
            TopologyVersion nonNullResponse = new TopologyVersion(processId, counter);

            nullResponse.CompareTopologyVersion(nonNullResponse).Should().BeNegative();
            nullResponse.IsStalerThanServerResponse(nonNullResponse).Should().BeTrue();
            nullResponse.IsFresherThanServerResponse(nonNullResponse).Should().BeFalse();

            nonNullResponse.CompareTopologyVersion(nullResponse).Should().BeNegative();
            nonNullResponse.IsStalerThanServerResponse(nullResponse).Should().BeTrue();
            nonNullResponse.IsFresherThanServerResponse(nullResponse).Should().BeFalse();

            nullResponse.CompareTopologyVersion(nullResponse).Should().BeNegative();
            nullResponse.IsStalerThanServerResponse(nullResponse).Should().BeTrue();
            nullResponse.IsFresherThanServerResponse(nullResponse).Should().BeFalse();
        }

        [Fact]
        public void CompareFreshnessToServerResponse_should_always_return_negative_if_processIds_are_not_equal()
        {
            var processId1 = ObjectId.Empty;
            var processId2 = ObjectId.GenerateNewId();
            var counter = 42L;

            var subject1 = new TopologyVersion(processId1, counter);
            var subject2 = new TopologyVersion(processId2, counter);
            var local = subject1;
            var serverResponse = subject2;

            local.CompareTopologyVersion(serverResponse).Should().BeNegative();
            local.IsStalerThan(serverResponse).Should().BeTrue();
            local.IsFresherThan(serverResponse).Should().BeFalse();

            // changing the order should change the results
            local = subject2;
            serverResponse = subject1;

            local.CompareTopologyVersion(serverResponse).Should().BeNegative();
            local.IsStalerThan(serverResponse).Should().BeTrue();
            local.IsFresherThan(serverResponse).Should().BeFalse();
        }

        [Fact]
        public void CompareFreshnessTo_should_return_expected_when_processIds_are_equal_and_one_has_bigger_counter()
        {
            var processId1 = ObjectId.Empty;
            var processId2 = ObjectId.Empty;
            var counter = 42L;

            var older = new TopologyVersion(processId1, counter);
            var newer = new TopologyVersion(processId2, counter + 1);

            older.CompareTopologyVersion(newer).Should().BeNegative();
            older.IsStalerThan(newer).Should().BeTrue();
            older.IsFresherThan(newer).Should().BeFalse();

            newer.CompareTopologyVersion(older).Should().BePositive();
            newer.IsStalerThan(older).Should().BeFalse();
            newer.IsFresherThan(older).Should().BeTrue();
        }

        [Fact]
        public void Equals_should_return_false_if_processIds_are_equal_and_counters_are_not_equal()
        {
            var processId = ObjectId.Empty;
            var counter = 42L;

            var subject1 = new TopologyVersion(processId, counter);
            var subject2 = new TopologyVersion(processId, counter + 1);

            subject1.Equals(subject2).Should().BeFalse();
            subject1.Equals((object)subject2).Should().BeFalse();
            (subject1 == subject2).Should().BeFalse();
            (subject1 != subject2).Should().BeTrue();
            subject1.GetHashCode().Should().NotBe(subject2.GetHashCode());

            subject2.Equals(subject1).Should().BeFalse();
            subject2.Equals((object)subject1).Should().BeFalse();
            (subject2 == subject1).Should().BeFalse();
            (subject2 != subject1).Should().BeTrue();
        }

        [Fact]
        public void Equals_should_return_false_if_processId_is_not_equal()
        {
            var processId1 = ObjectId.Empty;
            var processId2 = ObjectId.GenerateNewId();
            var counter = 42L;

            var subject1 = new TopologyVersion(processId1, counter);
            var subject2 = new TopologyVersion(processId2, counter);

            subject1.Equals(subject2).Should().BeFalse();
            subject1.Equals((object)subject2).Should().BeFalse();
            (subject1 == subject2).Should().BeFalse();
            (subject1 != subject2).Should().BeTrue();
            subject1.GetHashCode().Should().NotBe(subject2.GetHashCode());

            subject2.Equals(subject1).Should().BeFalse();
            subject2.Equals((object)subject1).Should().BeFalse();
            (subject2 == subject1).Should().BeFalse();
            (subject2 != subject1).Should().BeTrue();
        }

        [Fact]
        public void Equals_should_return_true_if_all_fields_are_equal()
        {
            var processId = ObjectId.Empty;
            var counter = 42L;

            var subject1 = new TopologyVersion(processId, counter);
            var subject2 = new TopologyVersion(processId, counter);

            subject1.Equals(subject2).Should().BeTrue();
            subject2.Equals(subject1).Should().BeTrue();

            subject1.Equals((object)subject2).Should().BeTrue();
            subject2.Equals((object)subject1).Should().BeTrue();

            subject1.GetHashCode().Should().Be(subject2.GetHashCode());
            subject2.GetHashCode().Should().Be(subject1.GetHashCode());

            (subject1 == subject2).Should().BeTrue();
            (subject2 == subject1).Should().BeTrue();

            (subject1 != subject2).Should().BeFalse();
            (subject2 != subject1).Should().BeFalse();
        }

        [Fact]
        public void FromBsonDocument_should_return_TopologyDescription_when_supplied_valid_BsonDocument()
        {
            var processId = ObjectId.Empty;
            var counter = 31L;
            var topologyVersionDocument = new BsonDocument {{"processId", processId}, {"counter", counter}};

            TopologyVersion subject = TopologyVersion.FromBsonDocument(topologyVersionDocument);

            subject.Should().NotBeNull();
        }

        [Fact]
        public void FromBsonDocument_should_return_null_when_supplied_invalid_BsonDocument()
        {
            var invalidTopologyVersionDocument = new BsonDocument("counter", 31);

            TopologyVersion subject = TopologyVersion.FromBsonDocument(invalidTopologyVersionDocument);

            subject.Should().BeNull();
        }

        [Fact]
        public void ServerResponse_should_always_be_fresher_when_topology_descriptions_are_equal()
        {
            var processId = ObjectId.Empty;
            var counter = 31L;

            var local = new TopologyVersion(processId, counter);
            var serverResponse = local;

            (local == serverResponse).Should().BeTrue();
            local.IsStalerThan(serverResponse).Should().BeTrue();
            local.IsFresherThan(serverResponse).Should().BeFalse();
        }

        [Fact]
        public void ToBsonDocument_should_return_expected()
        {
            var processId = ObjectId.Empty;
            var counter = 31L;

            var subject = new TopologyVersion(processId, counter);
            var document = subject.ToBsonDocument();

            document["counter"].Should().Be(counter);
            document["processId"].Should().Be(processId);
        }
    }
}
