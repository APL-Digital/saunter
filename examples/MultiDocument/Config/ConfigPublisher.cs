using System.Text.Json.Serialization;
using Saunter.AttributeProvider.Attributes;

namespace MultiDocument.Config;

public record ConfigValueChanged(
    string Key,
    // Honored by the generated schema through [JsonPropertyName] (see PropertyNameSelector in Program.cs).
    [property: JsonPropertyName("new_value")] string NewValue);

[AsyncApi("v1")]
public class ConfigPublisher
{
    [Channel("config.value.changed")]
    [SendOperation]
    public void PublishConfigChange(ConfigValueChanged change) { }
}
