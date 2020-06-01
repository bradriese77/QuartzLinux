using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuartzServiceLib
{
    public interface IQuartzServiceJob : IJob
    {
        ITrigger Schedule { get; }
        JobBuilder JobDetail { get; set; }

        string GetLogFile();
    }
}
