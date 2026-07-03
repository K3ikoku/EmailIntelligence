using System.Text;
using Microsoft.AspNetCore.Http;

namespace EmailIntelligence.Tests.TestSupport;

public static class HttpRequestFactory
{
    public static HttpRequest Json(string? json)
    {
        var context = new DefaultHttpContext();
        var request = context.Request;

        if (json is null) return request;
        var bytes = Encoding.UTF8.GetBytes(json);
        request.Body = new MemoryStream(bytes);
        request.ContentLength = bytes.Length;
        request.ContentType = "application/json";

        return request;
    }
}
