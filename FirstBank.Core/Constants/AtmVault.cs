using System;
using System.Collections.Generic;
using System.Text;

namespace FirstBank.Core.Constants
{
    public static class AtmVault
    {
        // This simulates the physical cash reserve inside the ATM hardware
        public static decimal CurrentPhysicalReserve { get; set; } = 5_000_000m;
    }
}
