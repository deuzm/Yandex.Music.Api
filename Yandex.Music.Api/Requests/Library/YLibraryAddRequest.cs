using System.Collections.Generic;
using System.Net;

using Yandex.Music.Api.Common;
using Yandex.Music.Api.Common.YLibrary;

namespace Yandex.Music.Api.Requests.Library
{
    internal class YLibraryAddRequest : YRequest
    {
        public YLibraryAddRequest(YAuthStorage storage) : base(storage)
        {
        }

        public YRequest Create(string id, YLibrarySection section, YLibrarySectionType type = YLibrarySectionType.Likes)
        {
            Dictionary<string, string> body = new Dictionary<string, string> {
                { $"{section.ToString().ToLower().TrimEnd('s')}-ids", id }
            };

            List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>> {
                YRequestHeaders.Get(YHeader.ContentType, "application/x-www-form-urlencoded")
            };

            FormRequest($"{YEndpoints.API}/users/{storage.User.Uid}/{type.ToString().ToLower()}/{section.ToString().ToLower()}/add-multiple", 
                WebRequestMethods.Http.Post, body: GetQueryString(body), headers: headers);

            return this;
        }
    }
}