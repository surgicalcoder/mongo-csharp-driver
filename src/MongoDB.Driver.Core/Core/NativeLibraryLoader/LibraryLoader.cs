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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.NativeLibraryLoader
{
    internal enum SupportedPlatform
    {
        Windows,
        Linux,
        MacOS
    }

    internal class LibraryLoader
    {
        private readonly INativeLibraryLoader _nativeLoader;

        public LibraryLoader(Func<SupportedPlatform, string> libraryLocator)
        {
            Ensure.IsNotNull(libraryLocator, nameof(libraryLocator));
            _nativeLoader = CreateNativeLoader(libraryLocator);
        }

        // public methods
        public T GetDelegate<T>(string name)
        {
            IntPtr ptr = _nativeLoader.GetFunctionPointer(name);
            if (ptr == IntPtr.Zero)
            {
                throw new TypeLoadException($"The function {name} was not found.");
            }

            return Marshal.GetDelegateForFunctionPointer<T>(ptr);
        }

        // private methods
        private INativeLibraryLoader CreateNativeLoader(Func<SupportedPlatform, string> libraryLocator)
        {
            var currentPlatform = GetCurrentPlatform();
            var libraryPath = libraryLocator(currentPlatform);
            return CreateNativeLoader(currentPlatform, libraryPath);
        }

        private INativeLibraryLoader CreateNativeLoader(SupportedPlatform currentPlatform, string libraryPath)
        {
            switch (currentPlatform)
            {
                case SupportedPlatform.Linux:
                    return new LinuxLibraryLoader(libraryPath);
                case SupportedPlatform.MacOS:
                    return new DarwinLibraryLoader(libraryPath);
                case SupportedPlatform.Windows:
                    return new WindowsLibraryLoader(libraryPath);
                default:
                    throw new Exception("Unexpected platform.");
            }
        }

        private SupportedPlatform GetCurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return SupportedPlatform.Windows;
            }
#if NETSTANDARD1_5
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return SupportedPlatform.Linux;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return SupportedPlatform.MacOS;
            }
#endif

            throw new InvalidOperationException("Current platform is not supported by LibraryLoader.");
        }
    }
}
