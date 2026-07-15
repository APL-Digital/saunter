using System;

namespace Saunter
{
    /// <summary>
    /// Describes the AsyncAPI <c>info</c> object: metadata about the API.
    /// </summary>
    public class AsyncApiInfoDescriptor
    {
        /// <summary>
        /// The AsyncAPI <c>info.version</c> field: the version of the application API being described
        /// (not the AsyncAPI specification version).
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// The AsyncAPI <c>info.title</c> field: the title of the application.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The AsyncAPI <c>info.description</c> field: a short description of the application.
        /// CommonMark syntax can be used for rich text representation.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The AsyncAPI <c>info.contact</c> object: contact information for the exposed API.
        /// </summary>
        public AsyncApiContactDescriptor? Contact { get; set; }

        /// <summary>
        /// The AsyncAPI <c>info.license</c> object: license information for the exposed API.
        /// </summary>
        public AsyncApiLicenseDescriptor? License { get; set; }

        /// <summary>
        /// The AsyncAPI <c>info.termsOfService</c> field: a URL to the terms of service for the API.
        /// </summary>
        public Uri? TermsOfService { get; set; }
    }

    /// <summary>
    /// Describes the AsyncAPI <c>contact</c> object: contact information for the exposed API.
    /// </summary>
    public class AsyncApiContactDescriptor
    {
        /// <summary>
        /// The AsyncAPI <c>contact.url</c> field: a URL pointing to the contact information.
        /// </summary>
        public Uri? Url { get; set; }

        /// <summary>
        /// The AsyncAPI <c>contact.name</c> field: the identifying name of the contact person or organization.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The AsyncAPI <c>contact.email</c> field: the email address of the contact person or organization.
        /// </summary>
        public string? Email { get; set; }
    }

    /// <summary>
    /// Describes the AsyncAPI <c>license</c> object: license information for the exposed API.
    /// </summary>
    public class AsyncApiLicenseDescriptor
    {
        /// <summary>
        /// The AsyncAPI <c>license.url</c> field: a URL to the license used for the API.
        /// </summary>
        public Uri? Url { get; set; }

        /// <summary>
        /// The AsyncAPI <c>license.name</c> field: the license name used for the API.
        /// </summary>
        public string? Name { get; set; }
    }
}
