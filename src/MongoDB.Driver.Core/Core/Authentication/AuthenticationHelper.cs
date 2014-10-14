﻿/* Copyright 2013-2014 MongoDB Inc.
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
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Authentication
{
    internal static class AuthenticationHelper
    {
        private static readonly UTF8Encoding __encoding = new UTF8Encoding(false, true);

        public static async Task AuthenticateAsync(IConnection connection, TimeSpan timeout, CancellationToken cancellationToken)
        {
            // authentication is currently broken on arbiters
            if (!connection.Description.IsMasterResult.IsArbiter)
            {
                foreach (var authenticator in connection.Settings.Authenticators)
                {
                    await authenticator.AuthenticateAsync(connection, timeout, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public static string MongoPasswordDigest(string username, SecureString password)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = __encoding.GetBytes(username + ":mongo:");
                md5.TransformBlock(bytes, 0, bytes.Length, null, 0);

                IntPtr unmanagedPassword = IntPtr.Zero;
                try
                {
                    unmanagedPassword = Marshal.SecureStringToBSTR(password);
                    var passwordChars = new char[password.Length];
                    GCHandle passwordCharsHandle = new GCHandle();
                    try
                    {
                        passwordCharsHandle = GCHandle.Alloc(passwordChars, GCHandleType.Pinned);
                        Marshal.Copy(unmanagedPassword, passwordChars, 0, passwordChars.Length);

                        var byteCount = __encoding.GetByteCount(passwordChars);
                        var passwordBytes = new byte[byteCount];
                        GCHandle passwordBytesHandle = new GCHandle();
                        try
                        {
                            passwordBytesHandle = GCHandle.Alloc(passwordBytesHandle, GCHandleType.Pinned);
                            __encoding.GetBytes(passwordChars, 0, passwordChars.Length, passwordBytes, 0);
                            md5.TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);
                            return BsonUtils.ToHexString(md5.Hash);
                        }
                        finally
                        {
                            Array.Clear(passwordBytes, 0, passwordBytes.Length);

                            if (passwordBytesHandle.IsAllocated)
                            {
                                passwordBytesHandle.Free();
                            }
                        }
                    }
                    finally
                    {
                        Array.Clear(passwordChars, 0, passwordChars.Length);

                        if (passwordCharsHandle.IsAllocated)
                        {
                            passwordCharsHandle.Free();
                        }
                    }
                }
                finally
                {
                    if (unmanagedPassword != IntPtr.Zero)
                    {
                        Marshal.ZeroFreeBSTR(unmanagedPassword);
                    }
                }
            }
        }
    }
}