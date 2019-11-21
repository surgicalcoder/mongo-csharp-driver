/* Copyright 2019–present MongoDB Inc.
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

namespace MongoDB.Driver.Core.Compression.Snappy
{
    internal static class Snappy64Adapter
    {
        // public methods
        public static SnappyStatus snappy_compress(IntPtr input, int input_length, IntPtr output, ref int output_length)
        {
            var ulong_output_length = (ulong)output_length;
            var status = Snappy64NativeMethods.snappy_compress(input, (ulong)input_length, output, ref ulong_output_length);
            output_length = (int)ulong_output_length;
            return status;
        }

        public static int snappy_max_compressed_length(int input_length)
        {
            return (int)Snappy64NativeMethods.snappy_max_compressed_length((ulong)input_length);
        }

        public static SnappyStatus snappy_uncompress(IntPtr input, int input_length, IntPtr output, ref int output_length)
        {
            var ulong_output_length = (ulong)output_length;
            var status = Snappy64NativeMethods.snappy_uncompress(input, (ulong)input_length, output, ref ulong_output_length);
            output_length = (int)ulong_output_length;
            return status;
        }

        public static SnappyStatus snappy_uncompressed_length(IntPtr input, int input_length, out int output_length)
        {
            var status = Snappy64NativeMethods.snappy_uncompressed_length(input, (ulong)input_length, out var ulongOutputLength);
            output_length = (int)ulongOutputLength;
            return status;
        }

        public static SnappyStatus snappy_validate_compressed_buffer(IntPtr input, int input_length)
        {
            return Snappy64NativeMethods.snappy_validate_compressed_buffer(input, (ulong)input_length);
        }
    }
}
