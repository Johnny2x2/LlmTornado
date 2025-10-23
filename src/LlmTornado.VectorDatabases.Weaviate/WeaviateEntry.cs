namespace LlmTornado.VectorDatabases.Weaviate;

/// <summary>
/// Represents a single entry (document) in a Weaviate collection.
/// </summary>
public class WeaviateEntry
{
    /// <summary>
    /// Unique identifier for the entry.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The content/document text.
    /// </summary>
    public string? Document { get; set; }

    /// <summary>
    /// Metadata associated with the entry.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// The vector embedding for the entry.
    /// </summary>
    public float[]? Embedding { get; set; }

    /// <summary>
    /// Distance/similarity score (used in query results).
    /// </summary>
    public float? Distance { get; set; }

    public WeaviateEntry(string id, string? document = null, Dictionary<string, object>? metadata = null, float[]? embedding = null, float? distance = null)
    {
        Id = id;
        Document = document;
        Metadata = metadata;
        Embedding = embedding;
        Distance = distance;
    }
}
