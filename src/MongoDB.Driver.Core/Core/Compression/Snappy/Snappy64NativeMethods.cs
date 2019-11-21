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
    internal static class Snappy64NativeMethods
    {
        private static readonly Lazy<LibraryLoader> __libraryLoader = new Lazy<LibraryLoader>(CreateLibraryLoader, isThreadSafe: true);
        private static readonly Lazy<Delegates64.snappy_compress> __snappy_compress = new Lazy<Delegates64.snappy_compress>(Get_snappy_compress_delegate, isThreadSafe: true);
        private static readonly Lazy<Delegates64.snappy_max_compressed_length> __snappy_max_compressed_length = new Lazy<Delegates64.snappy_max_compressed_length>(Get_snappy_max_compressed_length_delegate, isThreadSafe: true);
        private static readonly Lazy<Delegates64.snappy_uncompress> __snappy_uncompress= new Lazy<Delegates64.snappy_uncompress>(Get_snappy_uncompress_delegate, isThreadSafe: true);
        private static readonly Lazy<Delegates64.snappy_uncompressed_length> __snappy_uncompressed_length = new Lazy<Delegates64.snappy_uncompressed_length>(Get_snappy_uncompressed_length_delegate, isThreadSafe: true);
        private static readonly Lazy<Delegates64.snappy_validate_compressed_buffer> __snappy_validate_compressed_buffer = new Lazy<Delegates64.snappy_validate_compressed_buffer>(Get_snappy_validate_compressed_buffer_delegate, isThreadSafe: true);

        // public methods
        public static SnappyStatus snappy_compress(IntPtr input, ulong input_length, IntPtr output, ref ulong output_length)
        {
            return __snappy_compress.Value(input, (ulong)input_length, output, ref output_length);
        }

        public static ulong snappy_max_compressed_length(ulong input_length)
        {
            return __snappy_max_compressed_length.Value(input_length);
        }

        public static SnappyStatus snappy_uncompress(IntPtr input, ulong input_length, IntPtr output, ref ulong output_length)
        {
            return __snappy_uncompress.Value(input, input_length, output, ref output_length);
        }

        public static SnappyStatus snappy_uncompressed_length(IntPtr input, ulong input_length, out ulong output_length)
        {
            return __snappy_uncompressed_length.Value(input, input_length, out output_length);
        }

        public static SnappyStatus snappy_validate_compressed_buffer(IntPtr input, ulong input_length)
        {
            return __snappy_validate_compressed_buffer.Value(input, (ulong)input_length);
        }

        // private methods
        private static LibraryLoader CreateLibraryLoader()
        {
            string libraryLocator(SupportedPlatform currentPlatform)
            {
                switch (currentPlatform)
                {
                    case SupportedPlatform.Windows:
                        // TODO: find path to snappy64.dll
                        throw new NotImplementedException();

                    case SupportedPlatform.Linux: // TODO: add support for Linux and MacOS later
                    case SupportedPlatform.MacOS:
                    default:
                        throw new InvalidOperationException($"Snappy is not supported on the current platform: {currentPlatform}.");
                }
            }

            return new LibraryLoader(libraryLocator);
        }

        private static Delegates64.snappy_compress Get_snappy_compress_delegate()
        {
            return __libraryLoader.Value.GetDelegate<Delegates64.snappy_compress>(nameof(snappy_compress));
        }

        private static Delegates64.snappy_max_compressed_length Get_snappy_max_compressed_length_delegate()
        {
            return __libraryLoader.Value.GetDelegate<Delegates64.snappy_max_compressed_length>(nameof(snappy_max_compressed_length));
        }

        private static Delegates64.snappy_uncompress Get_snappy_uncompress_delegate()
        {
            return __libraryLoader.Value.GetDelegate<Delegates64.snappy_uncompress>(nameof(snappy_uncompress));
        }

        private static Delegates64.snappy_uncompressed_length Get_snappy_uncompressed_length_delegate()
        {
            return __libraryLoader.Value.GetDelegate<Delegates64.snappy_uncompressed_length>(nameof(snappy_uncompressed_length));
        }

        private static Delegates64.snappy_validate_compressed_buffer Get_snappy_validate_compressed_buffer_delegate()
        {
            return __libraryLoader.Value.GetDelegate<Delegates64.snappy_validate_compressed_buffer>(nameof(snappy_validate_compressed_buffer));
        }

        // nested types
        private class Delegates64
        {
            public delegate SnappyStatus snappy_compress(IntPtr input, ulong input_length, IntPtr output, ref ulong output_length);
            public delegate ulong snappy_max_compressed_length(ulong input_length);
            public delegate SnappyStatus snappy_uncompress(IntPtr input, ulong input_length, IntPtr output, ref ulong output_length);
            public delegate SnappyStatus snappy_uncompressed_length(IntPtr input, ulong input_length, out ulong output_length);
            public delegate SnappyStatus snappy_validate_compressed_buffer(IntPtr input, ulong input_length);
        }
    }
}
