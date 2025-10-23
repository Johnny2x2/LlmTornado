namespace LlmTornado.VectorDatabases.Weaviate;

/// <summary>
/// Configuration options for connecting to a Weaviate instance.
/// </summary>
public class WeaviateConfigurationOptions
{
    /// <summary>
    /// The URI of the Weaviate instance (e.g., "http://localhost:8080" or "https://your-instance.weaviate.network").
    /// </summary>
    public string Uri { get; set; }

    /// <summary>
    /// Optional API key for authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Optional additional headers for authentication (e.g., for OIDC or other auth mechanisms).
    /// </summary>
    public Dictionary<string, string>? AdditionalHeaders { get; set; }

    public WeaviateConfigurationOptions(string uri, string? apiKey = null)
    {
        Uri = uri;
        ApiKey = apiKey;
    }
}
