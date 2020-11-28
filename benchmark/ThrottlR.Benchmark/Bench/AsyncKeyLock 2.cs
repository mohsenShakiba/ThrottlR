// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.
// Thanks to https://github.com/SixLabors/ImageSharp.Web/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace ThrottlR
{
    /// <summary>
    /// The async key lock prevents multiple asynchronous threads acting upon the same object with the given key at the same time.
    /// It is designed so that it does not block unique requests allowing a high throughput.
    /// </summary>
    internal sealed class AsyncKeyLock2
    {
       private IDictionary<string, TinySemaphore> _semaphores = new Dictionary<string, TinySemaphore>();
       private object l = new object();
        /// <summary>
        /// Locks the current thread in read mode asynchronously.
        /// </summary>
        /// <param name="key">The key identifying the specific object to lock against.</param>
        /// <returns>
        /// The <see cref="Task{IDisposable}"/> that will release the lock.
        /// </returns>
        public async Task<IDisposable> ReaderLockAsync(string key)
        {
            TinySemaphore ts;
            lock (l)
            {
                if (_semaphores.TryGetValue(key, out ts))
                {
                }
                else
                {
                    ts = new TinySemaphore();
                    _semaphores[key] = ts;
                }
            }

            await ts.WaitAsync();

            return ts;
        }

        /// <summary>
        /// Locks the current thread in write mode asynchronously.
        /// </summary>
        /// <param name="key">The key identifying the specific object to lock against.</param>
        /// <returns>
        /// The <see cref="Task{IDisposable}"/> that will release the lock.
        /// </returns>
        public async Task<IDisposable> WriterLockAsync(string key)
        {
            TinySemaphore ts;
            lock (l)
            {
                if (_semaphores.TryGetValue(key, out ts))
                {
                }
                else
                {
                    ts = new TinySemaphore();
                    _semaphores[key] = ts;
                }
            }

            await ts.WaitAsync();

            return ts;
        }


    }
}
