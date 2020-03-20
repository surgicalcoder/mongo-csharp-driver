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
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// The AWS signature version 4.
    /// </summary>
    internal static class AwsSignatureVersion4
    {
        /// <summary>
        /// Creates authorization request.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="accessKey">The access key.</param>
        /// <param name="secretKey">The secret key.</param>
        /// <param name="securityToken">The security token.</param>
        /// <param name="salt">The salt.</param>
        /// <param name="host">The host.</param>
        /// <param name="authorizationHeader">The authorization header.</param>
        /// <param name="timestamp">The timestamp.</param>
        public static void CreateAuthorizationRequest(
            DateTime dateTime,
            string accessKey,
            string secretKey,
            string securityToken,
            byte[] salt,
            string host,
            out string authorizationHeader,
            out string timestamp)
        {
            var body = "Action=GetCallerIdentity&Version=2011-06-15";
            var region = GetRegion(host);
            var service = "sts";

            timestamp = dateTime.ToString("yyyyMMddTHHmmssZ");

            var datestamp = dateTime.ToString("yyyyMMdd");

            var requestHeaders = GetRequestHeaders(
                body: body,
                contentType: "application/x-www-form-urlencoded",
                host: host,
                timestamp: timestamp,
                sessionToken: securityToken,
                nonce: salt);

            var canonicalHeaders = GetCanonicalHeaders(requestHeaders);
            var signedHeaders = GetSignedHeaders(requestHeaders);

            var canonicalRequest = string.Join("\n", "POST", "/", "", canonicalHeaders, "", signedHeaders, Hash(body));
            var algorithm = "AWS4-HMAC-SHA256";
            var credentialScope = $"{datestamp}/{region}/{service}/aws4_request";
            var stringToSign = string.Join("\n", algorithm, timestamp, credentialScope, Hash(canonicalRequest));
            var signature = GetSignature(stringToSign, secretKey, datestamp, region, service);

            authorizationHeader = $"{algorithm} Credential={accessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
        }

        //private static methods
        private static string GetCanonicalHeaders(SortedDictionary<string, string> requestHeaders)
        {
            return string.Join("\n", requestHeaders.Select(x => $"{x.Key.ToLowerInvariant()}:{x.Value}"));
        }

        private static string GetRegion(string host)
        {
            if (host == "sts.amazonaws.com")
            {
                return "us-east-1";
            }

            var split = host.Split('.');
            if (split.Count() > 1)
            {
                return split[1];
            }

            return "us-east-1";
        }

        private static SortedDictionary<string, string> GetRequestHeaders(
            string body,
            string contentType,
            string host,
            string timestamp,
            string sessionToken,
            byte[] nonce)
        {
            var requestHeaders = new SortedDictionary<string, string>
            {
                ["Content-Type"] = contentType,
                ["Content-Length"] = body.Length.ToString(),
                ["Host"] = host,
                ["X-Amz-Date"] = timestamp,
                ["X-MongoDB-GS2-CB-Flag"] = "n",
                ["X-MongoDB-Server-Nonce"] = Convert.ToBase64String(nonce)
            };
            if (sessionToken != null)
            {
                requestHeaders["X-Amz-Security-Token"] = sessionToken;
            }

            return requestHeaders;
        }

        private static string GetSignature(string stringToSign, string secret, string date, string region, string service)
        {
            byte[] kDateBlock = Hmac256(Encoding.ASCII.GetBytes("AWS4" + secret), Encoding.ASCII.GetBytes(date));
            byte[] kRegionBlock = Hmac256(kDateBlock, Encoding.ASCII.GetBytes(region));
            byte[] kServiceBlock = Hmac256(kRegionBlock, Encoding.ASCII.GetBytes(service));
            byte[] kSigningBlock = Hmac256(kServiceBlock, Encoding.ASCII.GetBytes("aws4_request"));

            return BsonUtils.ToHexString(Hmac256(kSigningBlock, Encoding.ASCII.GetBytes(stringToSign)));
        }

        private static string GetSignedHeaders(SortedDictionary<string, string> requestHeaders)
        {
            return string.Join(";", requestHeaders.Keys.Select(x => x.ToLowerInvariant()));
        }

        private static string Hash(string str)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(str);
            using (SHA256 algorithm = SHA256.Create())
            {
                var hash = algorithm.ComputeHash(bytes);

                return BsonUtils.ToHexString(hash);
            }
        }

        private static byte[] Hmac256(byte[] keyBytes, byte[] bytes)
        {
            using (var hmac = new HMACSHA256(keyBytes))
            {
                return hmac.ComputeHash(bytes);
            }
        }
    }
}
