using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Yandex.Music.Api.Common;

namespace Yandex.Music.Api.Requests
{
    internal class YRequest
    {
        public YRequest(YAuthStorage userStorage)
        {
            storage = userStorage;
        }

        #region ����

        private HttpWebRequest fullRequest;
        protected YAuthStorage storage;

        #endregion ����

        #region ��������������� �������

        protected string GetQueryString(Dictionary<string, string> query)
        {
            return string.Join("&", query.Select(p => $"{p.Key}={HttpUtility.UrlEncode(p.Value)}"));
        }

        protected virtual void FormRequest(string url, string method = WebRequestMethods.Http.Get,
            Dictionary<string, string> query = null, List<KeyValuePair<string, string>> headers = null, string body = null)
        {
            var queryStr = string.Empty;
            if (query != null && query.Count > 0)
                queryStr = "?" + GetQueryString(query);

            var uri = new Uri($"{url}{queryStr}");
            var request = WebRequest.CreateHttp(uri);

            if (storage.Context.WebProxy != null)
                request.Proxy = storage.Context.WebProxy;

            request.Method = method;
            if (storage.Context.Cookies == null)
                storage.Context.Cookies = new CookieContainer();

            storage.SetHeaders(request);

            if (headers != null && headers.Count > 0)
                foreach (var header in headers)
                    request.Headers.Add(header.Key, header.Value);

            if (!string.IsNullOrEmpty(body)) {
                byte[] bytes = Encoding.UTF8.GetBytes(body);
                Stream s = request.GetRequestStream();
                s.Write(bytes, 0, bytes.Length);

                request.ContentLength = bytes.Length;
            }

            request.CookieContainer = storage.Context.Cookies;
            request.KeepAlive = true;
            request.Headers[HttpRequestHeader.AcceptCharset] = Encoding.UTF8.WebName;
            request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip";
            request.AutomaticDecompression = DecompressionMethods.GZip;

            fullRequest = request;
        }

        private JToken GetResultNode(JToken token)
        {
            return token["result"] ?? token;
        }

        protected T Deserialize<T>(JToken token, string jsonPath = "")
        {
            JToken result = GetResultNode(token).SelectToken(jsonPath);

            switch (result.Type) {
                case JTokenType.String:
                    return (T) Convert.ChangeType(result.ToString(), typeof(T));
                default:
                    return JsonConvert.DeserializeObject<T>(GetResultNode(token).SelectToken(jsonPath).ToString());
            }
        }

        protected T Deserialize<T>(string json, string jsonPath = "")
        {
            return Deserialize<T>(JToken.Parse(json), jsonPath);
        }

        protected List<T> DeserializeList<T>(JToken token, string jsonPath = "")
        {
            return GetResultNode(token).SelectTokens(jsonPath)
                .Select(t => JsonConvert.DeserializeObject<T>(t.ToString()))
                .ToList();
        }

        protected List<T> DeserializeList<T>(string json, string jsonPath = "")
        {
            return DeserializeList<T>(JToken.Parse(json), jsonPath);
        }

        protected async Task<T> GetDataFromResponseAsync<T>(HttpWebResponse response, string jsonPath = "")
        {
            try {
                string result;
                using (var stream = response.GetResponseStream()) {
                    var reader = new StreamReader(stream);
                    result = await reader.ReadToEndAsync();
                }

                storage.Context.Cookies.Add(response.Cookies);
                return Deserialize<T>(result, jsonPath);
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
                return default(T);
            }
        }

        protected async Task<List<T>> GetDataFromResponseAsyncList<T>(HttpWebResponse response, string jsonPath = "")
        {
            try {
                string result;
                using (var stream = response.GetResponseStream()) {
                    var reader = new StreamReader(stream);
                    result = await reader.ReadToEndAsync();
                }

                storage.Context.Cookies.Add(response.Cookies);
                return DeserializeList<T>(result, jsonPath);
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
                return new List<T>();
            }
        }

        #endregion ��������������� �������

        #region �������� �������

        public async Task<HttpWebResponse> GetResponseAsync()
        {
            try {
                return (HttpWebResponse) await fullRequest.GetResponseAsync();
            }
            catch (Exception ex) {
                using (StreamReader sr = new StreamReader(((WebException)ex).Response.GetResponseStream())) {
                    string result = await sr.ReadToEndAsync();
                    Console.WriteLine(result);
                }

                throw;
            }
        }

        public async Task<T> GetResponseAsync<T>(string jsonPath = "")
        {
            if (fullRequest == null)
                return default(T);

            using (var response = await GetResponseAsync()) {
                return await GetDataFromResponseAsync<T>(response, jsonPath);
            }
        }

        public async Task<List<T>> GetResponseAsyncList<T>(string jsonPath = "")
        {
            if (fullRequest == null)
                return new List<T>();

            using (var response = await GetResponseAsync()) {
                return await GetDataFromResponseAsyncList<T>(response, jsonPath);
            }
        }

        #endregion �������� �������
    }
}