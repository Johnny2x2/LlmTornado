namespace LlmTornado.VectorDatabases.Weaviate;

/// <summary>
/// Represents a Weaviate collection (class in Weaviate terminology).
/// </summary>
public class WeaviateCollection
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The dimension of vectors stored in this collection.
    /// </summary>
    public int VectorDimension { get; set; }

    /// <summary>
    /// Optional metadata associated with the collection.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    public WeaviateCollection(string name, int vectorDimension, Dictionary<string, object>? metadata = null)
    {
        Name = name;
        VectorDimension = vectorDimension;
        Metadata = metadata;
    }
}
