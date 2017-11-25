﻿/* Copyright 2010-2017 MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal class BulkDeleteOperation : BulkUnmixedWriteOperationBase
    {
        // constructors
        public BulkDeleteOperation(
            CollectionNamespace collectionNamespace,
            IEnumerable<DeleteRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, requests, messageEncoderSettings)
        {
        }

        // properties
        protected override string CommandName
        {
            get { return "delete"; }
        }

        public new IEnumerable<DeleteRequest> Requests
        {
            get { return base.Requests.Cast<DeleteRequest>(); }
        }

        protected override string RequestsElementName
        {
            get { return "deletes"; }
        }

        // methods
        protected override BatchSerializer CreateBatchSerializer(ConnectionDescription connectionDescription, int maxBatchCount, int maxBatchLength)
        {
            return new DeleteBatchSerializer(connectionDescription, maxBatchCount, maxBatchLength);
        }

        // nested types
        private class DeleteBatchSerializer : BatchSerializer
        {
            // constructors
            public DeleteBatchSerializer(ConnectionDescription connectionDescription, int maxBatchCount, int maxBatchLength)
                : base(connectionDescription, maxBatchCount, maxBatchLength)
            {
            }

            // methods
            protected override void SerializeRequest(BsonSerializationContext context, WriteRequest request)
            {
                var deleteRequest = (DeleteRequest)request;
                Feature.Collation.ThrowIfNotSupported(ConnectionDescription.ServerVersion, deleteRequest.Collation);

                var writer = context.Writer;
                writer.PushSettings(s => { var bs = s as BsonBinaryWriterSettings; if (bs != null) { bs.MaxDocumentSize = ConnectionDescription.MaxDocumentSize; } });
                try
                {
                    writer.WriteStartDocument();
                    writer.WriteName("q");
                    BsonSerializer.Serialize(writer, deleteRequest.Filter);
                    writer.WriteInt32("limit", deleteRequest.Limit);
                    if (deleteRequest.Collation != null)
                    {
                        writer.WriteName("collation");
                        BsonDocumentSerializer.Instance.Serialize(context, deleteRequest.Collation.ToBsonDocument());
                    }
                    writer.WriteEndDocument();
                }
                finally
                {
                    writer.PopSettings();
                }
            }
        }
    }
}
