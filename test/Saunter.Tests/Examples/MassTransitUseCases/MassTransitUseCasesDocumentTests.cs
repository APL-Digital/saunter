#nullable enable
using System.Linq;
using MassTransitUseCases.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Saunter.Options;
using Saunter.Tests.AttributeProvider.DocumentGenerationTests;
using Shouldly;
using Xunit;

namespace Saunter.Tests.Examples.MassTransitUseCases
{
    public class MassTransitUseCasesDocumentTests
    {
        [Fact]
        public void GeneratedDocument_ContainsThePrimaryUseCases()
        {
            var services = new ServiceCollection();
            services.AddFakeLogging();
            services.AddUseCaseAsyncApi();

            using var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<AsyncApiOptions>>().Value;
            var provider = serviceProvider.GetRequiredService<IAsyncApiDocumentProvider>();

            var document = provider.GetDocument(null, options);

            document.Info.Title.ShouldBe("MassTransit Use Cases");
            document.Servers.ShouldContainKey("inmemory");
            document.Servers.ShouldContainKey("rabbitmq");

            var catalogChannel = document.AssertAndGetChannel("pricesChanged", "catalog.prices.changed");
            var catalogSend = document.AssertAndGetOperation("Publish", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
            var catalogReceive = document.AssertAndGetOperation("Consume", ByteBard.AsyncAPI.Models.AsyncApiAction.Receive);
            document.AssertChannelMessages(catalogChannel, "productPriceChanged");
            document.AssertByMessage(catalogSend, "productPriceChanged");
            document.AssertByMessage(catalogReceive, "productPriceChanged");

            var inventoryChannel = document.AssertAndGetChannel("inventoryReservations", "inventory/{warehouseId}/reservations/request");
            inventoryChannel.Parameters.ShouldContainKey("warehouseId");
            document.Components.Parameters.ShouldContainKey("warehouseId");
            document.Components.Parameters["warehouseId"].DefaultValue.ShouldBe("primary");
            document.Components.Parameters["warehouseId"].Examples.ShouldBe(new[] { "primary", "overflow" });
            document.AssertChannelMessages(inventoryChannel, "inventoryReservationRequested");

            var requestOperation = document.AssertAndGetOperation("RequestInventoryReservation", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
            var handleOperation = document.AssertAndGetOperation("HandleInventoryReservation", ByteBard.AsyncAPI.Models.AsyncApiAction.Receive);
            document.AssertByMessage(requestOperation, "inventoryReservationRequested");
            document.AssertByMessage(handleOperation, "inventoryReservationRequested");

            requestOperation.Reply.ShouldNotBeNull();
            requestOperation.Reply.ChannelId.ShouldBe("inventoryReservationsReply");
            requestOperation.Reply.MessageIds.ShouldBe(new[] { "inventoryReserved" });

            handleOperation.Reply.ShouldNotBeNull();
            handleOperation.Reply.ChannelId.ShouldBe("inventoryReservationsReply");
            handleOperation.Reply.MessageIds.ShouldBe(new[] { "inventoryReserved" });

            var inventoryReplyChannel = document.AssertAndGetChannel("inventoryReservationsReply", null);
            document.AssertChannelMessages(inventoryReplyChannel, "inventoryReserved");

            var billingChannel = document.AssertAndGetChannel("billingLifecycle", "billing/invoices/lifecycle");
            var billingOperation = document.AssertAndGetOperation("PublishBillingLifecycleEvents", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
            document.AssertChannelMessages(billingChannel, "invoiceIssued", "invoicePaid");
            document.AssertByMessage(billingOperation, "invoiceIssued", "invoicePaid");

            var accountingChannel = document.AssertAndGetChannel("accountingEvents", "billing/accounting/events");
            var accountingOperation = document.AssertAndGetOperation("ConsumeAccountingEvents", ByteBard.AsyncAPI.Models.AsyncApiAction.Receive);
            document.AssertChannelMessages(accountingChannel, "paymentCaptured", "refundIssued");
            document.AssertByMessage(accountingOperation, "paymentCaptured", "refundIssued");

            var fulfillmentChannel = document.AssertAndGetChannel("fulfillmentPickPack", "fulfillment/pick-pack");
            var fulfillmentSend = document.AssertAndGetOperation("SendPickPackRequested", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
            var fulfillmentReceive = document.AssertAndGetOperation("HandlePickPackRequested", ByteBard.AsyncAPI.Models.AsyncApiAction.Receive);
            document.AssertChannelMessages(fulfillmentChannel, "pickPackRequested");
            document.AssertByMessage(fulfillmentSend, "pickPackRequested");
            document.AssertByMessage(fulfillmentReceive, "pickPackRequested");

            var searchChannel = document.AssertAndGetChannel("searchIndexSync", "search/indexes/{indexName}/sync");
            var searchOperation = document.AssertAndGetOperation("PublishSearchIndexSyncRequested", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
            searchChannel.Parameters.ShouldContainKey("indexName");
            document.AssertChannelMessages(searchChannel, "searchIndexSyncRequested");
            document.AssertByMessage(searchOperation, "searchIndexSyncRequested");
            searchChannel.BindingsRef.ShouldBe("searchIndexKafkaTopic");
            searchOperation.BindingsRef.ShouldBe("searchIndexKafkaProducer");
            document.Components.Messages["searchIndexSyncRequested"].BindingsRef.ShouldBe("searchIndexKafkaMessage");

            var exportChannel = document.AssertAndGetChannel("catalogExportLifecycle", "catalog/exports/lifecycle");
            var exportOperation = document.AssertAndGetOperation("PublishCatalogExportLifecycle", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
            document.AssertChannelMessages(exportChannel, "catalogExportStarted", "catalogExportCompleted");
            document.AssertByMessage(exportOperation, "catalogExportStarted", "catalogExportCompleted");

            var pricingChannel = document.AssertAndGetChannel("pricingQuoteRequests", "pricing/quotes/requests");
            var pricingRequest = document.AssertAndGetOperation("RequestPricingQuote", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
            var pricingReceive = document.AssertAndGetOperation("HandlePricingQuoteRequested", ByteBard.AsyncAPI.Models.AsyncApiAction.Receive);
            document.AssertChannelMessages(pricingChannel, "pricingQuoteRequested");
            document.AssertByMessage(pricingRequest, "pricingQuoteRequested");
            document.AssertByMessage(pricingReceive, "pricingQuoteRequested");
            pricingRequest.Reply.ShouldNotBeNull();
            pricingRequest.Reply.ChannelId.ShouldBe("pricingQuoteReplies");
            pricingReceive.Reply.ShouldNotBeNull();
            pricingReceive.Reply.ChannelId.ShouldBe("pricingQuoteReplies");

            var pricingReplyChannel = document.AssertAndGetChannel("pricingQuoteReplies", "pricing/quotes/replies");
            document.AssertChannelMessages(pricingReplyChannel, "pricingQuoteReady");

            var customerPreferenceChannel = document.AssertAndGetChannel("customerPreferences", "customers/preferences/changed");
            var customerPreferenceOperation = document.AssertAndGetOperation("HandleCustomerPreferenceChanged", ByteBard.AsyncAPI.Models.AsyncApiAction.Receive);
            document.AssertChannelMessages(customerPreferenceChannel, "customerPreferenceChanged");
            document.AssertByMessage(customerPreferenceOperation, "customerPreferenceChanged");

            var projectionChannel = document.AssertAndGetChannel("orderProjectionPipeline", "orders/projections/pipeline");
            var projectionReceive = document.AssertAndGetOperation("HandleOrderProjectionRequested", ByteBard.AsyncAPI.Models.AsyncApiAction.Receive);
            var projectionSend = document.AssertAndGetOperation("PublishOrderProjectionUpdated", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
            document.AssertChannelMessages(projectionChannel, "orderProjectionRequested", "orderProjectionUpdated");
            document.AssertByMessage(projectionReceive, "orderProjectionRequested");
            document.AssertByMessage(projectionSend, "orderProjectionUpdated");

            var complianceChannel = document.AssertAndGetChannel("complianceDecisions", "compliance/decisions");
            var complianceOperation = document.AssertAndGetOperation("PublishComplianceDecision", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
            document.AssertChannelMessages(complianceChannel, "complianceApproved", "complianceRejected");
            document.AssertByMessage(complianceOperation, "complianceApproved", "complianceRejected");
            document.Components.Messages["complianceApproved"].PayloadSchemaId.ShouldBe(document.Components.Messages["complianceRejected"].PayloadSchemaId);

            var geoInventoryChannel = document.AssertAndGetChannel("geoInventoryAdjusted", "inventory/{region}/adjusted");
            var geoInventoryOperation = document.AssertAndGetOperation("PublishGeoInventoryAdjusted", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
            geoInventoryChannel.Parameters.ShouldContainKey("region");
            document.Components.Parameters["region"].Location.ShouldBe("$message.header#/region");
            geoInventoryChannel.Tags.Select(tag => tag.Name).ShouldContain("inventory");
            document.AssertChannelMessages(geoInventoryChannel, "inventoryAdjusted");
            document.AssertByMessage(geoInventoryOperation, "inventoryAdjusted");
            geoInventoryOperation.Tags.ShouldContain("inventory");
            document.Components.Messages["inventoryAdjusted"].Tags.ShouldContain("inventory");

            var digestRequestChannel = document.AssertAndGetChannel("notificationDigestRequests", "notifications/digests/requests");
            var digestRequestOperation = document.AssertAndGetOperation("RequestNotificationDigest", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
            var digestHandleOperation = document.AssertAndGetOperation("HandleNotificationDigestRequest", ByteBard.AsyncAPI.Models.AsyncApiAction.Receive);
            document.AssertChannelMessages(digestRequestChannel, "notificationDigestRequested");
            document.AssertByMessage(digestRequestOperation, "notificationDigestRequested");
            document.AssertByMessage(digestHandleOperation, "notificationDigestRequested");
            digestRequestOperation.Reply.ShouldNotBeNull();
            digestRequestOperation.Reply.ChannelId.ShouldBe("notificationDigestReplies");
            digestRequestOperation.Reply.AddressLocation.ShouldBeNull();
            digestHandleOperation.Reply.ShouldNotBeNull();
            digestHandleOperation.Reply.ChannelId.ShouldBe("notificationDigestReplies");
            digestHandleOperation.Reply.AddressLocation.ShouldBeNull();

            var digestReplyChannel = document.AssertAndGetChannel("notificationDigestReplies", null);
            document.AssertChannelMessages(digestReplyChannel, "notificationDigestReady");

            var tenantChannel = document.AssertAndGetChannel("tenantCatalogRebuilt", "tenants/catalog/tenant-catalog-rebuilt");
            var tenantOperation = document.AssertAndGetOperation("PublishTenantCatalogRebuilt", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
            document.AssertChannelMessages(tenantChannel, "tenantCatalogRebuilt");
            document.AssertByMessage(tenantOperation, "tenantCatalogRebuilt");

            var partnerChannel = document.AssertAndGetChannel("partnerExportRequested", "partners/exports/requested");
            var partnerOperation = document.AssertAndGetOperation("PublishPartnerExportRequested", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
            document.AssertChannelMessages(partnerChannel, "partnerExportRequested");
            document.AssertByMessage(partnerOperation, "partnerExportRequested");
        }
    }
}
