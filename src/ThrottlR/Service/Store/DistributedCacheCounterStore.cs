using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace ThrottlR
{
    public class DistributedCacheCounterStore : ICounterStore
    {
        private readonly IDistributedCache _cache;
        private const int I32Len = 4;
        private const int I64Len = 8;

        public DistributedCacheCounterStore(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async ValueTask SetAsync(ThrottlerItem throttlerItem, Counter counter, TimeSpan? expirationTime, CancellationToken cancellationToken)
        {
            var options = new DistributedCacheEntryOptions();

            if (expirationTime.HasValue)
            {
                options.SetAbsoluteExpiration(expirationTime.Value);
            }

            var key = GenerateThrottlerItemKey(throttlerItem);
            await _cache.SetAsync(key, ToBinary(counter), options, cancellationToken);
        }

        public async ValueTask<Counter?> GetAsync(ThrottlerItem throttlerItem, CancellationToken cancellationToken)
        {
            var key = GenerateThrottlerItemKey(throttlerItem);
            var stored = await _cache.GetAsync(key, cancellationToken);

            if (stored != null)
            {
                return FromBinary(stored);
            }

            return default;
        }

        public async ValueTask RemoveAsync(ThrottlerItem throttlerItem, CancellationToken cancellationToken)
        {
            var key = GenerateThrottlerItemKey(throttlerItem);
            await _cache.RemoveAsync(key, cancellationToken);
        }

        public virtual string GenerateThrottlerItemKey(ThrottlerItem throttlerItem)
        {
            return throttlerItem.GenerateCounterKey("Throttler");
        }

        private static byte[] ToBinary(Counter counter)
        {

            var data = new byte[I32Len + I64Len];

            // no need to check for success, it only fails on size mismatch which shouldn't happen
            BitConverter.TryWriteBytes(data.AsSpan(default, I32Len), counter.Count);
            BitConverter.TryWriteBytes(data.AsSpan(I32Len, I64Len), counter.Timestamp.Ticks);
            return data;
        }

        private static Counter? FromBinary(byte[] counterBinaryData)
        {
            try
            {

                var counterBinaryDataAsSpan = counterBinaryData.AsSpan();
                var count = BitConverter.ToInt32(counterBinaryDataAsSpan.Slice(default, I32Len));
                var timestamp = new DateTime(BitConverter.ToInt64(counterBinaryDataAsSpan.Slice(I32Len, I64Len)));

                return new Counter(timestamp, count);
            }
            catch
            {
                return default;
            }
        }
    }

}
