using Google.Apis.Auth.OAuth2;
using Google.Apis.Json;
using GoogleCloudPrint.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudPrint
{
    public partial class GoogleCloudPrintService
    {
        private const string GoogleCloudPrintApiUrl = "https://www.google.com/cloudprint";
        private const string GoogleCloudPrintScope = "https://www.googleapis.com/auth/cloudprint";
        private readonly string _jsonCredencialFilePath;
        private readonly string _source;
        private readonly string _serviceAccountEmail;
        private readonly string _keyFilePath;
        private readonly string _keyFileSecret;
        private ServiceAccountCredential _credentials;
        public List<CloudPrinter> Printers = new List<CloudPrinter>();

        public GoogleCloudPrintService(string serviceAccountEmail, string keyFilePath, string keyFileSecret, string source)
        {
            _serviceAccountEmail = serviceAccountEmail;
            _keyFilePath = keyFilePath;
            _keyFileSecret = keyFileSecret;
            _source = source;
        }

        public GoogleCloudPrintService(string jsonCredencialFilePath, string source)
        {
            _jsonCredencialFilePath = jsonCredencialFilePath;
            _source = source;
        }

        public async Task<CloudPrintShare> PrinterUnShareAsync(string printerId, string email)
        {
            try
            {
                var p = new PostData();

                p.Parameters.Add(new PostDataParam { Name = "printerid", Value = printerId, Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "email", Value = email, Type = PostDataParamType.Field });

                return await GcpServiceCallAsync<CloudPrintShare>("unshare", p);
            }
            catch (Exception ex)
            {
                return new CloudPrintShare { success = false, Exception = ex };
            }
        }

        public async Task<CloudPrintShare> PrinterShareAsync(string printerId, string email, bool notify)
        {
            try
            {
                var p = new PostData();

                p.Parameters.Add(new PostDataParam { Name = "printerid", Value = printerId, Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "email", Value = email, Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "role", Value = "APPENDER", Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "skip_notification", Value = notify.ToString(), Type = PostDataParamType.Field });

                return await GcpServiceCallAsync<CloudPrintShare>("share", p);
            }
            catch (Exception ex)
            {
                return new CloudPrintShare { success = false, Exception = ex };
            }
        }

        public async Task<CloudPrintJob> PrintDocumentAsync(string printerId, string title, byte[] document, string mimeType)
        {
            var content = "data:" + mimeType + ";base64," + Convert.ToBase64String(document);
            return await PrintDocumentAsync(printerId, title, content, "dataUrl");
        }

        public async Task<CloudPrintJob> PrintDocumentAsync(string printerId, string title, string url)
        {
            return await PrintDocumentAsync(printerId, title, url, "url");
        }

        public async Task<CloudPrintJob> PrintDocumentAsync(string printerId, string title, string content, string contentType)
        {
            try
            {
                var p = new PostData();

                p.Parameters.Add(new PostDataParam { Name = "printerid", Value = printerId, Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "capabilities", Value = "{\"capabilities\":[{}]}", Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "contentType", Value = contentType, Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "title", Value = title, Type = PostDataParamType.Field });

                var contentValue = content;

                p.Parameters.Add(new PostDataParam { Name = "content", Type = PostDataParamType.Field, Value = contentValue });

                return await GcpServiceCallAsync<CloudPrintJob>("submit", p);
            }
            catch (Exception ex)
            {
                return new CloudPrintJob { success = false, Exception = ex };
            }
        }

        public async Task<CloudPrinters> GetPrintersAsync()
        {
            // clear internal data, will be reset if call succeeds
            Printers = new List<CloudPrinter>();

            try
            {
                var rv = await GcpServiceCallAsync<CloudPrinters>("search");
                if (rv != null)
                {
                    Printers = rv.printers;
                }

                return rv;
            }
            catch (Exception ex)
            {
                return new CloudPrinters { success = false, printers = new List<CloudPrinter>(), Exception = ex };
            }
        }

        public async Task<CloudPrintJob> ProcessInviteAsync(string printerId)
        {
            try
            {
                var p = new PostData();

                p.Parameters.Add(new PostDataParam { Name = "printerid", Value = printerId, Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "accept", Value = "true", Type = PostDataParamType.Field });


                return await GcpServiceCallAsync<CloudPrintJob>("processinvite", p);
            }
            catch (Exception ex)
            {
                return new CloudPrintJob { success = false, Exception = ex };
            }
        }

        public virtual X509Certificate2 GetCertificate()
        {
            return new X509Certificate2(
                _keyFilePath,
                _keyFileSecret,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
        }

        private async Task RefreshAccessTokenAsync()
        {
            if (_credentials == null)
            {
                _credentials = !string.IsNullOrEmpty(_jsonCredencialFilePath) ? await AuthorizeAsync(_jsonCredencialFilePath) : await AuthorizeAsync();
            }

            if (_credentials.Token.IsExpired(_credentials.Clock))
            {
                await _credentials.RequestAccessTokenAsync(CancellationToken.None);
            }
        }

        private async Task<T> GcpServiceCallAsync<T>(string restVerb, PostData p = null) where T : class
        {
            await RefreshAccessTokenAsync();

            var authCode = _credentials.Token.AccessToken;
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"{GoogleCloudPrintApiUrl}/{restVerb}?output=json"));

            request.Headers.Add("X-CloudPrint-Proxy", _source);
            request.Headers.Add("Authorization", "OAuth " + authCode);

            if (p != null)
            {
                var postData = p.GetPostData();
                var data = Encoding.UTF8.GetBytes(postData);

                var content = new MultipartFormDataContent(p.Boundary) { new ByteArrayContent(data) };

                request.Content = content;
            }

            // Get response
            HttpResponseMessage response = null;
            try
            {
                var client = new HttpClient();
                response = await client.SendAsync(request);
            }
            catch (WebException webEx)
            {
                if (webEx.Response is HttpWebResponse myResponse)
                {
                    var exResponseStream = myResponse.GetResponseStream();

                    if (exResponseStream == null)
                    {
                        throw;
                    }

                    var strm = new StreamReader(exResponseStream, Encoding.UTF8);
                    var resp = strm.ReadToEnd();

                    throw new Exception(resp);
                }
            }

            if (response == null)
            {
                throw new Exception("Response was null!");
            }

            var responseStream = response.Content;

            if (responseStream == null)
            {
                throw new Exception("Response stream was null!");
            }

            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }

        private async Task<ServiceAccountCredential> AuthorizeAsync()
        {
            using (var certificate = GetCertificate())
            {
                var credential = new ServiceAccountCredential(
                    new ServiceAccountCredential.Initializer(_serviceAccountEmail)
                    {
                        Scopes = new[] { GoogleCloudPrintScope }
                    }.FromCertificate(certificate));

                await credential.RequestAccessTokenAsync(CancellationToken.None);

                return credential;
            }
        }

        private async Task<ServiceAccountCredential> AuthorizeAsync(string jsonCredentialPath)
        {
            string[] scopes = { GoogleCloudPrintScope };

            using (var stream = new FileStream(jsonCredentialPath, FileMode.Open, FileAccess.Read))
            {
                var credentialParameters = NewtonsoftJsonSerializer.Instance.Deserialize<JsonCredentialParameters>(stream);

                if (credentialParameters.Type != "service_account"
                    || string.IsNullOrEmpty(credentialParameters.ClientEmail)
                    || string.IsNullOrEmpty(credentialParameters.PrivateKey))
                    throw new InvalidOperationException("JSON content does not represent valid service account credentials.");

                var credential = new ServiceAccountCredential(
                    new ServiceAccountCredential.Initializer(credentialParameters.ClientEmail)
                    {
                        Scopes = scopes
                    }.FromPrivateKey(credentialParameters.PrivateKey));

                // this does the magic for webform that need sync results and fails with async execution
                await credential.RequestAccessTokenAsync(CancellationToken.None);

                return credential;
            }
        }
    }
}
