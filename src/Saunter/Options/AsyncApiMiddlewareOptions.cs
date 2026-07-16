namespace Saunter.Options
{
    /// <summary>
    /// Route options for hosting an AsyncAPI document and its UI.
    /// </summary>
    public class AsyncApiMiddlewareOptions
    {
        /// <summary>
        /// The route which the AsyncAPI document will be hosted
        /// </summary>
        public string Route { get; set; } = "/asyncapi/asyncapi.json";

        /// <summary>
        /// The base URL for the AsyncAPI UI
        /// </summary>
        public string UiBaseRoute { get; set; } = "/asyncapi/ui/";

        /// <summary>
        /// The title of the AsyncAPI UI page. When not set, falls back to the document's
        /// <c>info.title</c>, and finally to <c>AsyncAPI</c>.
        /// </summary>
        public string? UiTitle { get; set; }

        /// <summary>
        /// Resolves the effective UI page title: the explicitly configured <paramref name="uiTitle"/>,
        /// otherwise the document's <c>info.title</c>, otherwise <c>AsyncAPI</c>.
        /// </summary>
        internal static string ResolveUiTitle(string? uiTitle, AsyncApiDocumentDescriptor? document)
        {
            return uiTitle ?? document?.Info?.Title ?? "AsyncAPI";
        }
    }
}
