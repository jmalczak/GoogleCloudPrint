using System.Collections.Generic;

namespace GoogleCloudPrint.Model
{
    public class CloudPrinters : CloudResponseBase
    {
        public bool success { get; set; }

        public List<CloudPrinter> printers { get; set; }
    }
}
