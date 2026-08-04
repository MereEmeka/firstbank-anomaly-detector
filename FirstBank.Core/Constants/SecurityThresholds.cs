using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace FirstBank.Core.Constants
{
    public class SecurityThresholds
    {
        public const decimal CriticalTransferLimit = 5_000_000m;
        public const decimal HighValueTransferLimit = 1_000_000m;
        public const decimal MaxBalanceDepletionRatio = 0.9m;

        public const int ScoreCriticalVolume = 50;
        public const int ScoreHighVolume = 25;
        public const int ScoreAccountDrain = 30;
        public const int ScoreGeographicAnomaly = 20;

        public const int FraudTriggerScore = 70;
        public const int MaximumRiskScore = 100;
    }
}
