namespace LlmTornado.VectorDatabases.Milvus;

/// <summary>
/// Represents an entry (document) in a Milvus collection.
/// </summary>
public class MilvusEntry
{
    /// <summary>
    /// The ID of the entry.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The document content.
    /// </summary>
    public string? Document { get; set; }

    /// <summary>
    /// Metadata associated with the entry.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// The vector embedding.
    /// </summary>
    public float[]? Embedding { get; set; }

    /// <summary>
    /// The distance/similarity score (used in query results).
    /// </summary>
    public float? Distance { get; set; }

    /// <summary>
    /// Initializes a new instance of the MilvusEntry class.
    /// </summary>
    /// <param name="id">The ID of the entry.</param>
    /// <param name="document">The document content.</param>
    /// <param name="metadata">Metadata associated with the entry.</param>
    /// <param name="embedding">The vector embedding.</param>
    /// <param name="distance">The distance/similarity score.</param>
    public MilvusEntry(
        string id, 
        string? document = null, 
        Dictionary<string, object>? metadata = null, 
        float[]? embedding = null, 
        float? distance = null)
    {
        Id = id;
        Document = document;
        Metadata = metadata;
        Embedding = embedding;
        Distance = distance;
    }
}
