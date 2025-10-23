namespace LlmTornado.VectorDatabases.Milvus;

/// <summary>
/// Represents a collection in Milvus vector database.
/// </summary>
public class MilvusCollection
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The dimension of vectors in this collection.
    /// </summary>
    public int VectorDimension { get; set; }

    /// <summary>
    /// Metadata associated with the collection.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Initializes a new instance of the MilvusCollection class.
    /// </summary>
    /// <param name="name">The name of the collection.</param>
    /// <param name="vectorDimension">The dimension of vectors in this collection.</param>
    /// <param name="metadata">Metadata associated with the collection.</param>
    public MilvusCollection(string name, int vectorDimension, Dictionary<string, object>? metadata = null)
    {
        Name = name;
        VectorDimension = vectorDimension;
        Metadata = metadata;
    }
}
