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
using MongoDB.Driver.Core.NativeLibraryLoader;

namespace MongoDB.Driver.Core.Compression.Snappy
{
    internal static class SnappyAdapter
    {
        // public methods
        public static SnappyStatus snappy_compress(byte[] input, int input_offset, int input_length, byte[] output, int output_offset, ref int output_length)
        {
            using (var pinnedInput = new PinnedBuffer(input, input_offset))
            using (var pinnedOutput = new PinnedBuffer(output, output_offset))
            {
                return Snappy64Adapter.snappy_compress(pinnedInput.IntPtr, input_length, pinnedOutput.IntPtr, ref output_length);
            }
        }

        public static int snappy_max_compressed_length(int input_length)
        {
            return Snappy64Adapter.snappy_max_compressed_length(input_length);
        }

        public static SnappyStatus snappy_uncompress(byte[] input, int input_offset, int input_length, byte[] output, int output_offset, ref int output_length)
        {
            using (var pinnedInput = new PinnedBuffer(input, input_offset))
            using (var pinnedOutput = new PinnedBuffer(output, output_offset))
            {
                return Snappy64Adapter.snappy_uncompress(pinnedInput.IntPtr, input_length, pinnedOutput.IntPtr, ref output_length);
            }
        }

        public static SnappyStatus snappy_uncompressed_length(byte[] input, int input_offset, int input_length, out int output_length)
        {
            using (var pinnedInput = new PinnedBuffer(input, input_offset))
            {
                return Snappy64Adapter.snappy_uncompressed_length(pinnedInput.IntPtr, input_length, out output_length);
            }
        }

        public static SnappyStatus snappy_validate_compressed_buffer(byte[] input, int input_offset, int input_length)
        {
            using (var pinnedInput = new PinnedBuffer(input, input_offset))
            {
                return Snappy64Adapter.snappy_validate_compressed_buffer(pinnedInput.IntPtr, input_length);
            }
        }
    }
}
