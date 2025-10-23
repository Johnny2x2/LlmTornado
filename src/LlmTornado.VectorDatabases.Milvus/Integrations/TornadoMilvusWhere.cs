namespace LlmTornado.VectorDatabases.Milvus.Integrations;

/// <summary>
/// Converts TornadoWhereOperator to Milvus-compatible metadata filters.
/// </summary>
internal class TornadoMilvusWhere
{
    private readonly TornadoWhereOperator _whereOperator;

    /// <summary>
    /// Initializes a new instance of the TornadoMilvusWhere class.
    /// </summary>
    /// <param name="whereOperator">The where operator to convert.</param>
    public TornadoMilvusWhere(TornadoWhereOperator whereOperator)
    {
        _whereOperator = whereOperator;
    }

    /// <summary>
    /// Converts the TornadoWhereOperator to a dictionary format suitable for Milvus.
    /// </summary>
    /// <returns>A dictionary representation of the where clause.</returns>
    public Dictionary<string, object> ToWhere()
    {
        return _whereOperator.ToWhere();
    }
}
