namespace MassTransitStreetlights.Contracts;

public class CommonHeaders
{
    // Gap: the target YAML models these headers through a reusable messageTrait.
    // Saunter currently needs the headers schema attached directly to each message.
    public int MyAppHeader { get; set; }
}
