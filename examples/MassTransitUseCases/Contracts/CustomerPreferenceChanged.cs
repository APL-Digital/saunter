namespace MassTransitUseCases.Contracts;

public class CustomerPreferenceChanged
{
    public Guid CustomerId { get; set; }

    public string PreferenceName { get; set; } = string.Empty;

    public string NewValue { get; set; } = string.Empty;
}
