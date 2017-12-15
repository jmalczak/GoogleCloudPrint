using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleCloudPrint
{
    public partial class GoogleCloudPrintService
    {
        internal class PostData
        {
            private const string Crlf = "\r\n";

            internal string Boundary { get; set; }

            internal List<PostDataParam> Parameters { get; set; }

            internal PostData()
            {
                // Get boundary, default is --AaB03x
                Boundary = "----CloudPrintFormBoundary-" + DateTime.UtcNow.Ticks;

                // The set of parameters
                Parameters = new List<PostDataParam>();
            }

            internal string GetPostData()
            {
                var sb = new StringBuilder();
                foreach (var p in Parameters)
                {
                    sb.Append("--" + Boundary).Append(Crlf);

                    if (p.Type == PostDataParamType.File)
                    {
                        sb.Append(string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"", p.Name, p.FileName)).Append(Crlf);
                        sb.Append("Content-Type: ").Append(p.FileMimeType).Append(Crlf);
                        sb.Append("Content-Transfer-Encoding: base64").Append(Crlf);
                        sb.Append("").Append(Crlf);
                        sb.Append(p.Value).Append(Crlf);
                    }
                    else
                    {
                        sb.Append(string.Format("Content-Disposition: form-data; name=\"{0}\"", p.Name)).Append(Crlf);
                        sb.Append("").Append(Crlf);
                        sb.Append(p.Value).Append(Crlf);
                    }
                }

                sb.Append("--" + Boundary + "--").Append(Crlf);

                return sb.ToString();
            }
        }

        internal enum PostDataParamType
        {
            Field,
            File
        }

        internal class PostDataParam
        {
            public string Name { get; set; }
            public string FileName { get; set; }
            public string FileMimeType { get; set; }
            public string Value { get; set; }
            public PostDataParamType Type { get; set; }

            public PostDataParam()
            {
                FileMimeType = "text/plain";
            }
        }
    }
}
