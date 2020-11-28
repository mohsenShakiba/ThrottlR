using System;
using System.Threading;

namespace ThrottlR
{
    public class TinySemaphore: SemaphoreSlim
    {
        public TinySemaphore(): base(0, 1)
        {

        }

        protected override void Dispose(bool disposing)
        {
            Release();
        }
    }
}
