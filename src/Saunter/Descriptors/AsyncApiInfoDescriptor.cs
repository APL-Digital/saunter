using System;

namespace Saunter
{
    public class AsyncApiInfoDescriptor
    {
        public string? Version { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public AsyncApiContactDescriptor? Contact { get; set; }

        public AsyncApiLicenseDescriptor? License { get; set; }

        public Uri? TermsOfService { get; set; }
    }

    public class AsyncApiContactDescriptor
    {
        public Uri? Url { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }
    }

    public class AsyncApiLicenseDescriptor
    {
        public Uri? Url { get; set; }

        public string? Name { get; set; }
    }
}
