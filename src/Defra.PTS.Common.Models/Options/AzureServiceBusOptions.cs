using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Models.Options
{
    public class AzureServiceBusOptions
    {
        public string SubmitQueueName { get; set; }
        public string UpdateQueueName { get; set; }
    }
}
