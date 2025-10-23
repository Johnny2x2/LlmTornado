using MC = Milvus.Client;

namespace LlmTornado.VectorDatabases.Milvus;

/// <summary>
/// Client for interacting with Milvus vector database.
/// </summary>
public class MilvusClient
{
    private readonly MC.MilvusClient _client;
    private readonly string? _database;

    /// <summary>
    /// Initializes a new instance of the MilvusClient class.
    /// </summary>
    /// <param name="options">Configuration options for the Milvus connection.</param>
    public MilvusClient(MilvusConfigurationOptions options)
    {
        _database = options.Database;
        
        // Create the Milvus client
        if (!string.IsNullOrEmpty(options.Username) && !string.IsNullOrEmpty(options.Password))
        {
            _client = new MC.MilvusClient(
                options.Host, 
                options.Username, 
                options.Password,
                options.Port, 
                options.UseSsl,
                options.Database);
        }
        else
        {
            _client = new MC.MilvusClient(
                options.Host, 
                options.Port, 
                options.UseSsl,
                options.Database);
        }
    }

    /// <summary>
    /// Lists all collections in the database.
    /// </summary>
    /// <returns>A list of collection names.</returns>
    public async Task<List<string>> ListCollectionsAsync()
    {
        var response = await _client.ShowCollectionsAsync();
        return response.CollectionNames.ToList();
    }

    /// <summary>
    /// Gets a collection by name.
    /// </summary>
    /// <param name="name">The name of the collection.</param>
    /// <returns>A MilvusCollection if it exists, null otherwise.</returns>
    public async Task<MilvusCollection?> GetCollectionAsync(string name)
    {
        try
        {
            var hasCollection = await _client.HasCollectionAsync(name);
            if (!hasCollection)
            {
                return null;
            }

            var describeResponse = await _client.DescribeCollectionAsync(name);
            
            // Extract vector dimension from the schema
            int vectorDimension = 1536; // Default dimension
            foreach (var field in describeResponse.Schema.Fields)
            {
                if (field.DataType == MC.DataType.FloatVector)
                {
                    if (field.TypeParams != null)
                    {
                        foreach (var param in field.TypeParams)
                        {
                            if (param.Key == "dim" && int.TryParse(param.Value, out var dim))
                            {
                                vectorDimension = dim;
                                break;
                            }
                        }
                    }
                    break;
                }
            }

            return new MilvusCollection(name, vectorDimension);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a new collection.
    /// </summary>
    /// <param name="name">The name of the collection.</param>
    /// <param name="vectorDimension">The dimension of vectors in this collection.</param>
    /// <param name="metadata">Metadata associated with the collection.</param>
    /// <returns>The created MilvusCollection.</returns>
    public async Task<MilvusCollection> CreateCollectionAsync(string name, int vectorDimension, Dictionary<string, object>? metadata = null)
    {
        // Define the schema for the collection
        var schema = new MC.CollectionSchema
        {
            Fields =
            {
                MC.FieldSchema.Create<string>("id", isPrimaryKey: true, maxLength: 256),
                MC.FieldSchema.Create<string>("document", maxLength: 65535),
                MC.FieldSchema.CreateVarchar("metadata", maxLength: 65535),
                MC.FieldSchema.CreateFloatVector("embedding", vectorDimension)
            }
        };

        // Create the collection
        await _client.CreateCollectionAsync(name, schema);

        // Create an index on the vector field for efficient similarity search
        var indexParams = new MC.IndexParams
        {
            IndexType = MC.IndexType.IvfFlat,
            MetricType = MC.SimilarityMetricType.Cosine,
            ExtraParams = { ["nlist"] = "1024" }
        };

        await _client.CreateIndexAsync(name, "embedding", indexParams);

        // Load the collection into memory
        await _client.LoadCollectionAsync(name);

        return new MilvusCollection(name, vectorDimension, metadata);
    }

    /// <summary>
    /// Gets or creates a collection.
    /// </summary>
    /// <param name="name">The name of the collection.</param>
    /// <param name="vectorDimension">The dimension of vectors in this collection.</param>
    /// <param name="metadata">Metadata associated with the collection.</param>
    /// <returns>The MilvusCollection.</returns>
    public async Task<MilvusCollection> GetOrCreateCollectionAsync(string name, int vectorDimension, Dictionary<string, object>? metadata = null)
    {
        var collection = await GetCollectionAsync(name);
        if (collection != null)
        {
            return collection;
        }

        return await CreateCollectionAsync(name, vectorDimension, metadata);
    }

    /// <summary>
    /// Deletes a collection.
    /// </summary>
    /// <param name="name">The name of the collection to delete.</param>
    public async Task DeleteCollectionAsync(string name)
    {
        await _client.DropCollectionAsync(name);
    }

    /// <summary>
    /// Gets the internal Milvus client.
    /// </summary>
    internal MC.MilvusClient GetInternalClient() => _client;

    /// <summary>
    /// Gets the database name.
    /// </summary>
    internal string? GetDatabase() => _database;
}
