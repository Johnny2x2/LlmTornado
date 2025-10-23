namespace LlmTornado.VectorDatabases.Pinecone;

/// <summary>
/// Configuration options for connecting to Pinecone vector database.
/// </summary>
public class PineconeConfigurationOptions
{
    /// <summary>
    /// Gets or sets the Pinecone API key.
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the Pinecone environment (optional, for serverless indexes).
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PineconeConfigurationOptions"/> class.
    /// </summary>
    /// <param name="apiKey">The Pinecone API key.</param>
    /// <param name="environment">The Pinecone environment (optional).</param>
    public PineconeConfigurationOptions(string apiKey, string? environment = null)
    {
        ApiKey = apiKey;
        Environment = environment;
    }
}
