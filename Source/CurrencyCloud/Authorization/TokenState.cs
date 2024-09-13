using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyCloud.Authorization
{
    public sealed class TokenState
    {
        public string Token { get; set; }
        public DateTime LastTokenRefresh { get; set; }
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
    }
}
