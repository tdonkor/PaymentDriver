using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Acrelec.Library.Logger;
using ECRUtilATLLib;

namespace Acrelec.Mockingbird.Payment
{
    public class GetTerminalStatus
    {
        StatusClass termimalStatus;

        public string GetTheTerminalStatus()
        {
            string status = string.Empty;
            termimalStatus = new StatusClass();

            termimalStatus.GetTerminalState(); 
            status = Utils.DisplayTerminalStatus(Convert.ToInt16(termimalStatus.StateOut));

            return status;
        }
    }
}
