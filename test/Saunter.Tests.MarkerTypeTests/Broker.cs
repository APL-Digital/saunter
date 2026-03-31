using Saunter.AttributeProvider.Attributes;

namespace Saunter.Tests.MarkerTypeTests
{
    [AsyncApi]
    [Channel("asw.sample_service.anothersample", "asw.sample_service.anothersample", Description = "Another sample events.")]
    [SendOperation(OperationId = "AnotherSampleMessagePublisher", Summary = "Send another sample.")]
    public class AnotherSamplePublisher
    {
        [Message(typeof(AnotherSampleMesssage))]
        public void PublishTenantCreated(AnotherSampleMesssage _) { }
    }

    [AsyncApi]
    [Channel("asw.sample_service.sample", "asw.sample_service.sample", Description = "Sample events.")]
    [ReceiveOperation(OperationId = "SampleMessageConsumer", Summary = "Receive sample messages.")]
    public class SampleConsumer
    {
        [Message(typeof(SampleMessage))]
        public void SubscribeSampleMessage(SampleMessage _) { }
    }

    public class SampleMessage { }
    public class AnotherSampleMesssage { }
}
