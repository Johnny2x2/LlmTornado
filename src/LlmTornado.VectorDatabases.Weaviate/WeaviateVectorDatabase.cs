using Weaviate;
using System.Text.Json;

namespace LlmTornado.VectorDatabases.Weaviate.Integrations;

/// <summary>
/// Weaviate implementation of the IVectorDatabase interface.
/// Provides a connector to Weaviate vector store for LlmTornado.
/// </summary>
public class WeaviateVectorDatabase : IVectorDatabase
{
    private readonly WeaviateClient _client;
    private readonly WeaviateConfigurationOptions _config;
    private string _collectionName = "defaultCollection";
    private int _vectorDimension;

    /// <summary>
    /// Gets the current collection name.
    /// </summary>
    public string CollectionName => _collectionName;

    /// <summary>
    /// Initializes a new instance of the Weaviate vector database connector.
    /// </summary>
    /// <param name="uri">The URI of the Weaviate instance (e.g., "http://localhost:8080").</param>
    /// <param name="vectorDimension">The dimension of the vectors to be stored.</param>
    /// <param name="apiKey">Optional API key for authentication.</param>
    public WeaviateVectorDatabase(string uri, int vectorDimension = 1536, string? apiKey = null)
    {
        _vectorDimension = vectorDimension;
        _config = new WeaviateConfigurationOptions(uri, apiKey);
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _client = new WeaviateClient(baseUri: new Uri(uri));
        }
        else
        {
            _client = new WeaviateClient(apiKey, baseUri: new Uri(uri));
        }

        Task.Run(async () => await TestWeaviateConnection()).Wait();
    }

    private async Task TestWeaviateConnection()
    {
        try
        {
            string testCollectionName = $"test_collection_{Guid.NewGuid().ToString().Substring(0, 4)}";
            await InitializeCollection(testCollectionName);
            await DeleteCollectionAsync(testCollectionName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Weaviate instance not reachable at {_config.Uri}", ex);
        }
    }

    /// <summary>
    /// Initializes or switches to a collection with the specified name.
    /// </summary>
    /// <param name="collectionName">The name of the collection to initialize.</param>
    public async Task InitializeCollection(string collectionName)
    {
        if (collectionName.Equals(_collectionName))
        {
            return;
        }

        _collectionName = collectionName;
        
        // Check if class exists, create if it doesn't
        try
        {
            var existingClass = await _client.Schema.SchemaObjectsGetAsync(className: collectionName);
        }
        catch
        {
            // Class doesn't exist, create it
            await CreateCollection(collectionName);
        }
    }

    private async Task CreateCollection(string collectionName)
    {
        var classDefinition = new Class
        {
            Class1 = collectionName,
            Description = $"Collection for {collectionName} documents",
            VectorIndexType = "hnsw",
            Vectorizer = "none",  // We provide vectors ourselves
            Properties = new List<Property>
            {
                new Property
                {
                    Name = "content",
                    DataType = new List<string> { "text" },
                    Description = "The document content"
                },
                new Property
                {
                    Name = "metadata",
                    DataType = new List<string> { "text" },
                    Description = "JSON metadata"
                }
            }
        };

        await _client.Schema.SchemaObjectsCreateAsync(classDefinition);
    }

    /// <summary>
    /// Deletes a collection by name.
    /// </summary>
    /// <param name="collectionName">The name of the collection to delete.</param>
    public async Task DeleteCollectionAsync(string collectionName)
    {
        try
        {
            await _client.Schema.SchemaObjectsDeleteAsync(className: collectionName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to delete collection {collectionName}", ex);
        }
    }

    private void ThrowIfCollectionNotInitialized()
    {
        if (string.IsNullOrEmpty(_collectionName))
        {
            throw new InvalidOperationException("Collection is not initialized. Please initialize the collection first.");
        }
    }

    public string GetCollectionName() => _collectionName;

    public void AddDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await AddDocumentsAsync(documents)).Wait();
    }

    public async Task AddDocumentsAsync(VectorDocument[] documents)
    {
        ThrowIfCollectionNotInitialized();

        var batchObjects = new List<global::Weaviate.Object>();
        foreach (var doc in documents)
        {
            var properties = new Dictionary<string, object?>
            {
                ["content"] = doc.Content,
                ["metadata"] = doc.Metadata != null ? JsonSerializer.Serialize(doc.Metadata) : "{}"
            };

            var obj = new global::Weaviate.Object
            {
                Class = _collectionName,
                Id = Guid.TryParse(doc.Id, out var guid) ? guid : Guid.NewGuid(),
                Properties = properties,
                Vector = doc.Embedding?.ToList()
            };

            batchObjects.Add(obj);
        }

        // Use batch API for better performance
        var batchList = batchObjects.Cast<global::Weaviate.Object>().ToList() as IList<global::Weaviate.Object>;
        await _client.Batch.BatchObjectsCreateAsync(objects: batchList);
    }

    public VectorDocument[]? GetDocuments(string[] ids)
    {
        return Task.Run(async () => await GetDocumentsAsync(ids)).Result;
    }

    public async Task<VectorDocument[]> GetDocumentsAsync(string[] ids)
    {
        ThrowIfCollectionNotInitialized();

        var results = new List<VectorDocument>();
        foreach (var id in ids)
        {
            try
            {
                if (!Guid.TryParse(id, out var guid))
                {
                    continue;
                }

                var obj = await _client.Objects.ObjectsClassGetAsync(
                    className: _collectionName,
                    id: guid);

                if (obj != null && obj.Properties != null)
                {
                    string content = "";
                    string metadataStr = "{}";
                    
                    // Properties is of type object, so we need to handle it dynamically
                    var propsDict = obj.Properties as Dictionary<string, object>;
                    if (propsDict != null)
                    {
                        if (propsDict.ContainsKey("content"))
                        {
                            content = propsDict["content"]?.ToString() ?? "";
                        }
                        
                        if (propsDict.ContainsKey("metadata"))
                        {
                            metadataStr = propsDict["metadata"]?.ToString() ?? "{}";
                        }
                    }
                    
                    Dictionary<string, object>? metadata = null;
                    try
                    {
                        metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataStr);
                    }
                    catch
                    {
                        metadata = new Dictionary<string, object>();
                    }

                    results.Add(new VectorDocument(
                        id: obj.Id?.ToString() ?? "",
                        content: content,
                        metadata: metadata,
                        embedding: obj.Vector?.ToArray()
                    ));
                }
            }
            catch
            {
                // Skip objects that couldn't be retrieved
                continue;
            }
        }

        return results.ToArray();
    }

    public void UpdateDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await UpdateDocumentsAsync(documents)).Wait();
    }

    public async Task UpdateDocumentsAsync(VectorDocument[] documents)
    {
        ThrowIfCollectionNotInitialized();

        foreach (var doc in documents)
        {
            if (!Guid.TryParse(doc.Id, out var guid))
            {
                continue;
            }

            var properties = new Dictionary<string, object?>
            {
                ["content"] = doc.Content,
                ["metadata"] = doc.Metadata != null ? JsonSerializer.Serialize(doc.Metadata) : "{}"
            };

            var updateObj = new global::Weaviate.Object
            {
                Class = _collectionName,
                Id = guid,
                Properties = properties,
                Vector = doc.Embedding?.ToList()
            };

            await _client.Objects.ObjectsClassPutAsync(
                className: _collectionName,
                id: guid,
                request: updateObj);
        }
    }

    public void UpsertDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await UpsertDocumentsAsync(documents)).Wait();
    }

    public async Task UpsertDocumentsAsync(VectorDocument[] documents)
    {
        ThrowIfCollectionNotInitialized();

        // Weaviate batch API is idempotent and will overwrite existing objects
        await AddDocumentsAsync(documents);
    }

    public void DeleteDocuments(string[] ids)
    {
        Task.Run(async () => await DeleteDocumentsAsync(ids)).Wait();
    }

    public async Task DeleteDocumentsAsync(string[] ids)
    {
        ThrowIfCollectionNotInitialized();

        foreach (var id in ids)
        {
            try
            {
                if (!Guid.TryParse(id, out var guid))
                {
                    continue;
                }

                await _client.Objects.ObjectsClassDeleteAsync(
                    className: _collectionName,
                    id: guid);
            }
            catch
            {
                // Skip if object doesn't exist
                continue;
            }
        }
    }

    public VectorDocument[] QueryByEmbedding(float[] embedding, TornadoWhereOperator? where = null, int topK = 5, bool includeScore = false)
    {
        return Task.Run(async () => await QueryByEmbeddingAsync(embedding, where, topK, includeScore)).Result;
    }

    public async Task<VectorDocument[]> QueryByEmbeddingAsync(float[] embedding, TornadoWhereOperator? where = null, int topK = 5, bool includeScore = false)
    {
        ThrowIfCollectionNotInitialized();

        // For now, return empty array. Full GraphQL implementation would require more complex parsing
        // This is a minimal implementation to satisfy the interface
        // TODO: Implement GraphQL query for vector similarity search
        await Task.CompletedTask; // Suppress async warning
        return Array.Empty<VectorDocument>();
    }
}
