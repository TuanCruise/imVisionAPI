using System.Text.Json.Serialization;

namespace ImVision.Integration.Dtos;

public class ImVisionEventDto
{
    [JsonPropertyName("eventType")]
    public int EventType { get; set; }

    [JsonPropertyName("subId")]
    public int? SubId { get; set; }

    [JsonPropertyName("eventTypeName")]
    public string? EventTypeName { get; set; }

    [JsonPropertyName("relatedElements")]
    public object? RelatedElements { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("resourceTypeName")]
    public string? ResourceTypeName { get; set; }

    [JsonPropertyName("concreteAssetType")]
    public string? ConcreteAssetType { get; set; }

    [JsonPropertyName("concreteAssetTypeId")]
    public int ConcreteAssetTypeId { get; set; }

    [JsonPropertyName("parentId")]
    public int ParentId { get; set; }
}
