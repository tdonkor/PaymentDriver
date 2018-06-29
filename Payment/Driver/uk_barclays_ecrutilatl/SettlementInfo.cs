using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acrelec.Mockingbird.Payment
{
  
        public enum SettlementParam : short
        {
            CURRENT_SETTLEMENT = 0,
            PREVIOUS_SETTLEMENT = 1,
        }

        public enum SettlementResult : short
        {
            SETTLEMENT_NONE = 0,
            SETTLEMENT_AGREED = 1,
            SETTLEMENT_NOT_AGREED = 2,
            SETTLEMENT_UNCONFIRMED = 3,
            SETTLEMENT_CANNOT_CONFIRM = 4,
            SETTLEMENT_STOREFULL = 5,
            SETTLEMENT_CANCELLED = 6,
            SETTLEMENT_UNKNOWN = 7,
        }

        public struct SettlementRequest
        {
            public short AcquirerIndex;
            public SettlementParam SettlementParameter;
        }
   
}
