/* Copyright 2018-present MongoDB Inc.
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

namespace MongoDB.Driver.Core.Misc
{
    internal class LifoPool<TItem>
    {
        private readonly object _lock = new object();
        private readonly List<TItem> _items = new List<TItem>();

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _items.Count;
                }
            }
        }

        public void Release(TItem item)
        {
            lock (_lock)
            {
                _items.Add(item);
            }
        }

        public bool ReleaseAndTryRemoveMatchingLeastRecentlyUsed(TItem item, Func<TItem, bool> predicate, out TItem matchingItem)
        {
            lock(_lock)
            {
                Release(item);
                return TryRemoveMatchingLeastRecentlyUsed(predicate, out matchingItem);
            }
        }

        public bool TryAcquire(out TItem item)
        {
            lock (_lock)
            {
                var count = _items.Count;
                if (count > 0)
                {
                    var index = count - 1;
                    item = _items[index];
                    _items.RemoveAt(index);
                    return true;
                }
            }

            item = default(TItem);
            return false;
        }

        public bool TryRemoveMatchingLeastRecentlyUsed(Func<TItem, bool> predicate, out TItem matchingItem)
        {
            lock (_lock)
            {
                if (_items.Count > 0)
                {
                    var item = _items[0];
                    if (predicate(item))
                    {
                        _items.RemoveAt(0);
                        matchingItem = item;
                        return true;
                    }
                }
            }

            matchingItem = default(TItem);
            return false;
        }
    }
}
