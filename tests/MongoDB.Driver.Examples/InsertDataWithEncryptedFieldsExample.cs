/* Copyright 2020-present MongoDB Inc.
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
using System.IO;
using MongoDB.Bson;
using MongoDB.Driver.Encryption;
using Xunit;

namespace MongoDB.Driver.Examples
{
    public class InsertDataWithEncryptedFields
    {
        private static readonly string ConnectionString = "mongodb://localhost:27017";
        private static readonly string SampleNameValue = "John Doe";
        private static readonly int SampleSsnValue = 145014000;

        private static BsonDocument SampleDoc =>
            new BsonDocument
            {
                { "name", SampleNameValue },
                { "ssn", SampleSsnValue },
                { "bloodType", "AB-" },
                {
                    "medicalRecords",
                    new BsonArray(new []
                    {
                        new BsonDocument("weight", 180),
                        new BsonDocument("bloodPressure", "120/80")
                    })
                },
                {
                    "insurance",
                    new BsonDocument
                    {
                        { "policyNumber", 123142 },
                        { "provider", "MaestCare" }
                    }
                }
            };

        [Fact]
        public void AutoEncryption()
        {
            var keyVaultCollectionNamespace = CollectionNamespace.FromFullName("encryption.__keyVault");
            var recordsCollectionNamespace = CollectionNamespace.FromFullName("medicalRecords.patients");

            var base64KeyId = ""; // paste the generated key in Standard GuidRepresentation form here

            var ssnQuery = Builders<BsonDocument>.Filter.Eq("ssn", SampleSsnValue);

            // Construct a JSON Schema
            var schema = JsonSchemaCreator.CreateJsonSchema(base64KeyId);

            // Construct an encrypted client
            var encryptedClient = CreateEncryptedClient(
                recordsCollectionNamespace,
                keyVaultCollectionNamespace,
                schema);
            var collection = encryptedClient
                .GetDatabase(recordsCollectionNamespace.DatabaseNamespace.DatabaseName)
                .GetCollection<BsonDocument>(recordsCollectionNamespace.CollectionName);

            // Insert a document into the collection
            collection.UpdateOne(ssnQuery, new BsonDocument("$set", SampleDoc), new UpdateOptions() { IsUpsert = true });
            Console.WriteLine("Successfully upserted the sample document!");

            // Query SSN field with encrypted client
            var result = collection.Find(ssnQuery).Single();

            Console.WriteLine("Encrypted client query by the SSN (deterministically-encrypted) field:\n" + result.ToJson());

            // Query SSN field with normal client without encryption
            var normalMongoClient = new MongoClient(ConnectionString);
            collection = normalMongoClient
              .GetDatabase(recordsCollectionNamespace.DatabaseNamespace.DatabaseName)
              .GetCollection<BsonDocument>(recordsCollectionNamespace.CollectionName);
            var normalClientResult = collection.Find(ssnQuery).FirstOrDefault();
            if (normalClientResult != null)
            {
                throw new Exception("Assert that the filtered data has not been found.");
            }

            // Query name (non-encrypted) field with normal client without encryption
            var nameQuery = Builders<BsonDocument>.Filter.Eq("name", SampleNameValue);
            var normalClientNameResult = collection.Find(nameQuery).FirstOrDefault();
            if (normalClientNameResult == null)
            {
                throw new Exception("Assert that the filtered data has been found.");
            }

            Console.WriteLine($"Query by name returned the following document:\n {normalClientNameResult}.");
        }

        private IMongoClient CreateEncryptedClient(
            CollectionNamespace recordNamespace,
            CollectionNamespace keyVaultNamespace,
            BsonDocument schema)
        {
            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>();

            // For local master key
            var localMasterKey = File.ReadAllText("master-key.txt");
            var localMasterKeyBytes = new BsonBinaryData(Convert.FromBase64String(localMasterKey)).Bytes;

            var localOptions = new Dictionary<string, object>
            {
                { "key", localMasterKeyBytes }
            };
            kmsProviders.Add("local", localOptions);

            /* For Aws KMS, uncomment this block.
            var awsAccessKey = "<Aws access key>";
            var awsSecretAccessKey = "<Aws secret access key>";
            var awsKmsOptions = new Dictionary<string, object>
            {
                { "accessKeyId", awsAccessKey },
                { "secretAccessKey", awsSecretAccessKey }
            };
            kmsProviders.Add("aws", awsKmsOptions);
            */

            /* For Azure KMS, uncomment this block.
            var azureTenantId = "<Azure account organization>";
            var azureClientId = "<Azure client ID>";
            var azureClientSecret = "<Azure client secret>";
            var azureKmsOptions = new Dictionary<string, object>
            {
                { "tenantId", azureTenantId },
                { "clientId", azureClientId },
                { "clientSecret", azureClientSecret }
            };
            kmsProviders.Add("azure", azureKmsOptions);
            */

            /* For Gcp KMS, uncomment this block.
            var gcpEmail = "<Gcp email>";
            var gcpPrivateKey = "<Gcp private key>";
            var gcpKmsOptions = new Dictionary<string, object>
            {
                { "email", gcpEmail },
                { "privateKey", gcpPrivateKey }
            };
            kmsProviders.Add("gcp", gcpKmsOptions);
            */

            var schemaMap = new Dictionary<string, BsonDocument>();
            schemaMap.Add(recordNamespace.ToString(), schema);

            var extraOptions = new Dictionary<string, object>()
            {
                /* 
                 *   uncomment the following line if you are running mongocryptd manually
                { "mongocryptdBypassSpawn", true }
                */
            };

            var connectionString = "mongodb://localhost:27017";
            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            var autoEncryptionSettings = new AutoEncryptionOptions(
                keyVaultNamespace: keyVaultNamespace,
                kmsProviders: kmsProviders,
                schemaMap: schemaMap,
                extraOptions: extraOptions);
            mongoClientSettings.AutoEncryptionOptions = autoEncryptionSettings;
            return new MongoClient(mongoClientSettings);
        }
    }

    public static class JsonSchemaCreator
    {
        private static readonly string DETERMINISTIC_ENCRYPTION_TYPE = "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic";
        private static readonly string RANDOM_ENCRYPTION_TYPE = "AEAD_AES_256_CBC_HMAC_SHA_512-Random";

        private static BsonDocument BuildEncryptMetadata(string base64KeyId)
        {
            var guid = GuidConverter.FromBytes(Convert.FromBase64String(base64KeyId), GuidRepresentation.Standard);
            var binary = new BsonBinaryData(guid, GuidRepresentation.Standard);
            return new BsonDocument("keyId", new BsonArray(new[] { binary }));
        }

        private static BsonDocument BuildEncryptedField(string bsonType, bool isDeterministic)
        {
            return new BsonDocument
            {
                {
                    "encrypt",
                    new BsonDocument
                    {
                        { "bsonType", bsonType },
                        { "algorithm", isDeterministic ? DETERMINISTIC_ENCRYPTION_TYPE : RANDOM_ENCRYPTION_TYPE}
                    }
                }
            };
        }

        public static BsonDocument CreateJsonSchema(string keyId)
        {
            return new BsonDocument
            {
                { "bsonType", "object" },
                { "encryptMetadata", BuildEncryptMetadata(keyId) },
                {
                    "properties",
                    new BsonDocument
                    {

                        { "ssn", BuildEncryptedField("int", true) },
                        { "bloodType", BuildEncryptedField("string", false) },
                        { "medicalRecords", BuildEncryptedField("array", false) },
                        {
                            "insurance",
                            new BsonDocument
                            {
                                { "bsonType", "object" },
                                {
                                    "properties",
                                    new BsonDocument
                                    {
                                        { "policyNumber", BuildEncryptedField("int", true) }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
