namespace GoogleCloudPrint.Model
{
    public class CloudPrinter : CloudResponseBase
    {
        public string id { get; set; }

        public string name { get; set; }

        public string description { get; set; }

        public string proxy { get; set; }

        public string status { get; set; }

        public string capsHash { get; set; }

        public string createTime { get; set; }

        public string updateTime { get; set; }

        public string accessTime { get; set; }

        public bool confirmed { get; set; }

        public int numberOfDocuments { get; set; }

        public int numberOfPages { get; set; }
    }
}
