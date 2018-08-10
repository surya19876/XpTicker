using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XpTicker
{
    public class ProcessDetails
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public TimeSpan TimeElapsed { get; set; }
    }
}
