namespace MassTransitUseCases.AsyncApi;

internal static class CommerceChannels
{
    public const string CatalogPriceChangedAddress = "catalog.prices.changed";

    public const string InventoryReservations = "inventoryReservations";
    public const string InventoryReservationsAddress = "inventory/{warehouseId}/reservations/request";
    public const string InventoryReservationsReply = "inventoryReservationsReply";

    public const string BillingLifecycle = "billingLifecycle";
    public const string BillingLifecycleAddress = "billing/invoices/lifecycle";

    public const string AccountingEvents = "accountingEvents";
    public const string AccountingEventsAddress = "billing/accounting/events";

    public const string FulfillmentPickPack = "fulfillmentPickPack";
    public const string FulfillmentPickPackAddress = "fulfillment/pick-pack";

    public const string SearchIndexSync = "searchIndexSync";
    public const string SearchIndexSyncAddress = "search/indexes/{indexName}/sync";

    public const string CatalogExportLifecycle = "catalogExportLifecycle";
    public const string CatalogExportLifecycleAddress = "catalog/exports/lifecycle";

    public const string PricingQuoteRequests = "pricingQuoteRequests";
    public const string PricingQuoteRequestsAddress = "pricing/quotes/requests";
    public const string PricingQuoteReplies = "pricingQuoteReplies";

    public const string CustomerPreferences = "customerPreferences";
    public const string CustomerPreferencesAddress = "customers/preferences/changed";

    public const string OrderProjectionPipeline = "orderProjectionPipeline";
    public const string OrderProjectionPipelineAddress = "orders/projections/pipeline";

    public const string ComplianceDecisions = "complianceDecisions";
    public const string ComplianceDecisionsAddress = "compliance/decisions";

    public const string GeoInventoryAdjusted = "geoInventoryAdjusted";
    public const string GeoInventoryAdjustedAddress = "inventory/{region}/adjusted";

    public const string NotificationDigestRequests = "notificationDigestRequests";
    public const string NotificationDigestRequestsAddress = "notifications/digests/requests";
    public const string NotificationDigestReplies = "notificationDigestReplies";

    public const string TenantCatalogRebuilt = "tenantCatalogRebuilt";

    public const string PartnerExportRequested = "partnerExportRequested";
    public const string PartnerExportRequestedAddress = "partners/exports/requested";
}
