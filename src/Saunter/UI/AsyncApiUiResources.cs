using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Saunter.UI
{
    internal static class AsyncApiUiResources
    {
        private static readonly EmbeddedFileProvider FileProvider = new(typeof(AsyncApiUiMiddleware).Assembly, typeof(AsyncApiUiMiddleware).Namespace);
        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();
        private static readonly ConcurrentDictionary<string, string> EmbeddedTextCache = new();

        public static async Task RespondWithHtml(HttpResponse response, string title, string documentUrl, string cssUrl, string jsUrl)
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = MediaTypeNames.Text.Html;
            await response.WriteAsync(RenderHtml(title, documentUrl, cssUrl, jsUrl), Encoding.UTF8);
        }

        public static string RenderHtml(string title, string documentUrl, string cssUrl, string jsUrl)
        {
            var template = EmbeddedTextCache.GetOrAdd(
                $"{typeof(AsyncApiUiMiddleware).Namespace}.index.html",
                ReadEmbeddedText);

            var indexHtml = new StringBuilder(template);

            // Values are encoded for the context they are substituted into in index.html:
            // title/css/js land in HTML (element text and attribute values); the document
            // URL is emitted inside a single-quoted JavaScript string literal.
            foreach (var replacement in new Dictionary<string, string>
            {
                ["{{title}}"] = HtmlEncoder.Default.Encode(title),
                ["{{asyncApiDocumentUrl}}"] = JavaScriptEncoder.Default.Encode(documentUrl),
                ["{{asyncApiUiCssUrl}}"] = HtmlEncoder.Default.Encode(cssUrl),
                ["{{asyncApiUiJsUrl}}"] = HtmlEncoder.Default.Encode(jsUrl),
            })
            {
                indexHtml.Replace(replacement.Key, replacement.Value);
            }

            return indexHtml.ToString();
        }

        public static async Task RespondWithEmbeddedAsset(HttpResponse response, string assetPath)
        {
            var file = FileProvider.GetFileInfo(assetPath);
            if (!file.Exists)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            if (!ContentTypeProvider.TryGetContentType(assetPath, out var contentType))
            {
                contentType = MediaTypeNames.Application.Octet;
            }

            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = contentType;
            response.ContentLength = file.Length;

            await using var stream = file.CreateReadStream();
            await stream.CopyToAsync(response.Body);
        }

        private static string ReadEmbeddedText(string resourceName)
        {
            using var stream = typeof(AsyncApiUiMiddleware).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException(
                    $"Embedded AsyncAPI UI resource '{resourceName}' was not found in assembly '{typeof(AsyncApiUiMiddleware).Assembly.GetName().Name}'.");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
