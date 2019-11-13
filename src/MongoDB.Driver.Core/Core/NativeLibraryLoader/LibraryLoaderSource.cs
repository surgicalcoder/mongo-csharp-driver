﻿/* Copyright 2019-present MongoDB Inc.
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

using MongoDB.Driver.Core.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MongoDB.Driver.Core.NativeLibraryLoader
{
    internal enum SupportedPlatforms
    {
        Windows,
        Linux,
        MacOs
    }

    internal interface ILibraryLoaderSource
    {
        T GetFunction<T>(string name);
    }

    internal class LibraryLoaderSource : ILibraryLoaderSource
    {
        private readonly string _libraryName;
        private readonly Lazy<IPlatformLibraryLoader> _loader;

        public LibraryLoaderSource(IDictionary<SupportedPlatforms, string> libraryRelativePaths, Func<bool, string> getLibraryNameFunc)
        {
            _libraryName = Ensure.IsNotNull(getLibraryNameFunc(Is64BitnessPlatform), nameof(getLibraryNameFunc));
            Ensure.IsNotNull(libraryRelativePaths, nameof(libraryRelativePaths));

            _loader = new Lazy<IPlatformLibraryLoader>(() =>
            {
                var currentPlatform = GetCurrentPlatform();
                if (currentPlatform.HasValue && libraryRelativePaths.TryGetValue(currentPlatform.Value, out var relativePath))
                {
                    return CreateLibraryLoader(currentPlatform.Value, relativePath);
                }
                else
                {
                    throw new PlatformNotSupportedException("The current platform is not supported or the library path has not been provided.");
                }
            }, isThreadSafe: true);
        }

        public LibraryLoaderSource(IDictionary<SupportedPlatforms, string> libraryRelativePaths, string libraryName)
            : this(libraryRelativePaths, b => libraryName)
        {
        }

        public bool Is64BitnessPlatform
        {
            get
            {
#if NET452 || NETSTANDARD2_0
                var is64Bit = Environment.Is64BitProcess;
#else
                var is64Bit = IntPtr.Size == 8;
#endif
                return is64Bit;
            }
        }

        // public methods
        public T GetFunction<T>(string name)
        {
            return GetFunction<T>(_loader.Value, name);
        }

        // private methods
        private IPlatformLibraryLoader CreateLibraryLoader(SupportedPlatforms supportedPlatform, string relativePath)
        {
            var path = GetAbsolutePath(relativePath);
            switch (supportedPlatform)
            {
                case SupportedPlatforms.Linux:
                    return new Linux.NativeMethods(path);
                case SupportedPlatforms.MacOs:
                    return new macOS.NativeMethods(path);
                case SupportedPlatforms.Windows:
                    return new Windows.NativeMethods(path);
                default:
                    throw new PlatformNotSupportedException("The platforms must be Windows, Linux or macOS.");
            }
        }

        private string FindLibraryOrThrow(IList<string> basePaths, params string[] suffixPaths)
        {
            var candidates = new List<string>();
            foreach (var basePath in basePaths)
            {
                foreach (var suffix in suffixPaths)
                {
                    var path = Path.Combine(basePath, suffix, _libraryName);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                    candidates.Add(path);
                }
            }

            throw new FileNotFoundException($"Could not find {_libraryName} \n Tried: {string.Join(",", candidates)}.");
        }

        private string GetAbsolutePath(string relativePath)
        {
            var candidatePaths = new List<string>();

            var assembly = typeof(LibraryLoaderSource).GetTypeInfo().Assembly;
            var location = assembly.Location;
            var basepath = Path.GetDirectoryName(location);
            candidatePaths.Add(basepath);

            return FindLibraryOrThrow(candidatePaths, relativePath);
        }

        private SupportedPlatforms? GetCurrentPlatform()
        {
#if NETSTANDARD1_5
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return SupportedPlatforms.MacOs;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return SupportedPlatforms.Linux;
            }
            else
#endif
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return SupportedPlatforms.Windows;
            }
            else
            {
                return null;
            }
        }

        private T GetFunction<T>(IPlatformLibraryLoader loader, string name)
        {
            IntPtr ptr = loader.GetFunction(name);
            if (ptr == IntPtr.Zero)
            {
                throw new TypeLoadException($"The function {name} has not been loaded.");
            }

            return Marshal.GetDelegateForFunctionPointer<T>(ptr);
        }
    }
}
