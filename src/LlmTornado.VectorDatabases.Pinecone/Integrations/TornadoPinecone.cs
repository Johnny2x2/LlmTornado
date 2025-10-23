using Pinecone;

namespace LlmTornado.VectorDatabases.Pinecone.Integrations;

/// <summary>
/// Pinecone vector database implementation of IVectorDatabase interface.
/// </summary>
public class TornadoPinecone : IVectorDatabase
{
    private readonly PineconeClient _pineconeClient;
    private readonly string _apiKey;
    private IndexClient? _index;
    private string _indexName = "default-index";
    private string _namespace = "";
    private readonly int _vectorDimension;

    /// <summary>
    /// Gets the current collection (index) name.
    /// </summary>
    public string CollectionName => _indexName;

    /// <summary>
    /// Initializes a new instance of the <see cref="TornadoPinecone"/> class.
    /// </summary>
    /// <param name="apiKey">The Pinecone API key.</param>
    /// <param name="vectorDimension">The dimension of the vectors (default: 1536).</param>
    public TornadoPinecone(string apiKey, int vectorDimension = 1536)
    {
        _apiKey = apiKey;
        _vectorDimension = vectorDimension;
        _pineconeClient = new PineconeClient(apiKey);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TornadoPinecone"/> class with configuration options.
    /// </summary>
    /// <param name="options">Configuration options for Pinecone.</param>
    /// <param name="vectorDimension">The dimension of the vectors (default: 1536).</param>
    public TornadoPinecone(PineconeConfigurationOptions options, int vectorDimension = 1536)
        : this(options.ApiKey, vectorDimension)
    {
    }

    /// <summary>
    /// Initializes the collection (index) with the specified name.
    /// </summary>
    /// <param name="indexName">The name of the index to use.</param>
    /// <param name="namespaceName">Optional namespace within the index (default: empty string).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeCollectionAsync(string indexName, string namespaceName = "")
    {
        _indexName = indexName;
        _namespace = namespaceName;

        try
        {
            // Try to describe the index to see if it exists
            await _pineconeClient.DescribeIndexAsync(indexName);
            _index = _pineconeClient.Index(indexName);
        }
        catch
        {
            // If index doesn't exist, create it
            await _pineconeClient.CreateIndexAsync(new CreateIndexRequest
            {
                Name = indexName,
                Dimension = _vectorDimension,
                Metric = CreateIndexRequestMetric.Cosine,
                Spec = new ServerlessIndexSpec
                {
                    Serverless = new ServerlessSpec
                    {
                        Cloud = ServerlessSpecCloud.Aws,
                        Region = "us-east-1"
                    }
                }
            });

            // Get the index reference
            _index = _pineconeClient.Index(indexName);
        }
    }

    /// <summary>
    /// Deletes the specified collection (index).
    /// </summary>
    /// <param name="indexName">The name of the index to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteCollectionAsync(string indexName)
    {
        await _pineconeClient.DeleteIndexAsync(indexName);
        if (_indexName == indexName)
        {
            _index = null;
        }
    }

    /// <inheritdoc/>
    public string GetCollectionName() => _indexName;

    /// <inheritdoc/>
    public void AddDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await AddDocumentsAsync(documents)).Wait();
    }

    /// <inheritdoc/>
    public async Task AddDocumentsAsync(VectorDocument[] documents)
    {
        ThrowIfIndexNotInitialized();

        var vectors = new List<Vector>();
        foreach (var doc in documents)
        {
            var vector = new Vector
            {
                Id = doc.Id,
                Values = doc.Embedding ?? Array.Empty<float>(),
                Metadata = ConvertMetadata(doc)
            };
            vectors.Add(vector);
        }

        await _index!.UpsertAsync(new UpsertRequest { Vectors = vectors, Namespace = _namespace });
    }

    /// <inheritdoc/>
    public VectorDocument[]? GetDocuments(string[] ids)
    {
        return Task.Run(async () => await GetDocumentsAsync(ids)).Result;
    }

    /// <inheritdoc/>
    public async Task<VectorDocument[]> GetDocumentsAsync(string[] ids)
    {
        ThrowIfIndexNotInitialized();

        var fetchResponse = await _index!.FetchAsync(new FetchRequest { Ids = ids.ToList(), Namespace = _namespace });
        var results = new List<VectorDocument>();

        if (fetchResponse.Vectors != null)
        {
            foreach (var vector in fetchResponse.Vectors.Values)
            {
                var doc = ConvertToVectorDocument(vector);
                if (doc != null)
                {
                    results.Add(doc);
                }
            }
        }

        return results.ToArray();
    }

    /// <inheritdoc/>
    public void UpsertDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await UpsertDocumentsAsync(documents)).Wait();
    }

    /// <inheritdoc/>
    public async Task UpsertDocumentsAsync(VectorDocument[] documents)
    {
        ThrowIfIndexNotInitialized();

        var vectors = new List<Vector>();
        foreach (var doc in documents)
        {
            var vector = new Vector
            {
                Id = doc.Id,
                Values = doc.Embedding ?? Array.Empty<float>(),
                Metadata = ConvertMetadata(doc)
            };
            vectors.Add(vector);
        }

        await _index!.UpsertAsync(new UpsertRequest { Vectors = vectors, Namespace = _namespace });
    }

    /// <inheritdoc/>
    public void UpdateDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await UpdateDocumentsAsync(documents)).Wait();
    }

    /// <inheritdoc/>
    public async Task UpdateDocumentsAsync(VectorDocument[] documents)
    {
        // Pinecone doesn't have a separate update operation, it uses upsert
        await UpsertDocumentsAsync(documents);
    }

    /// <inheritdoc/>
    public void DeleteDocuments(string[] ids)
    {
        Task.Run(async () => await DeleteDocumentsAsync(ids)).Wait();
    }

    /// <inheritdoc/>
    public async Task DeleteDocumentsAsync(string[] ids)
    {
        ThrowIfIndexNotInitialized();
        await _index!.DeleteAsync(new DeleteRequest { Ids = ids.ToList(), Namespace = _namespace });
    }

    /// <inheritdoc/>
    public VectorDocument[] QueryByEmbedding(float[] embedding, TornadoWhereOperator? where = null, int topK = 5, bool includeScore = false)
    {
        return Task.Run(async () => await QueryByEmbeddingAsync(embedding, where, topK, includeScore)).Result;
    }

    /// <inheritdoc/>
    public async Task<VectorDocument[]> QueryByEmbeddingAsync(float[] embedding, TornadoWhereOperator? where = null, int topK = 5, bool includeScore = false)
    {
        ThrowIfIndexNotInitialized();

        Metadata? filter = null;
        if (where != null)
        {
            var whereHelper = new TornadoPineconeWhere(where);
            filter = whereHelper.ToMetadata();
        }

        var queryResponse = await _index!.QueryAsync(new QueryRequest
        {
            Vector = embedding.ToArray(),
            TopK = (uint)topK,
            Filter = filter,
            Namespace = _namespace,
            IncludeMetadata = true,
            IncludeValues = false
        });

        var results = new List<VectorDocument>();
        if (queryResponse.Matches != null)
        {
            foreach (var match in queryResponse.Matches)
            {
                var doc = ConvertToVectorDocument(match);
                if (doc != null)
                {
                    if (includeScore)
                    {
                        doc.Score = match.Score;
                    }
                    results.Add(doc);
                }
            }
        }

        return results.ToArray();
    }

    private void ThrowIfIndexNotInitialized()
    {
        if (_index == null)
        {
            throw new InvalidOperationException("Index is not initialized. Please call InitializeCollectionAsync first.");
        }
    }

    private static Metadata ConvertMetadata(VectorDocument doc)
    {
        var metadata = new Metadata();
        
        // Add content as metadata
        if (!string.IsNullOrEmpty(doc.Content))
        {
            metadata["content"] = doc.Content;
        }

        // Add other metadata
        if (doc.Metadata != null)
        {
            foreach (var kvp in doc.Metadata)
            {
                if (kvp.Value is string str)
                {
                    metadata[kvp.Key] = str;
                }
                else if (kvp.Value is int intVal)
                {
                    metadata[kvp.Key] = intVal;
                }
                else if (kvp.Value is long longVal)
                {
                    metadata[kvp.Key] = longVal;
                }
                else if (kvp.Value is float floatVal)
                {
                    metadata[kvp.Key] = floatVal;
                }
                else if (kvp.Value is double doubleVal)
                {
                    metadata[kvp.Key] = doubleVal;
                }
                else if (kvp.Value is bool boolVal)
                {
                    metadata[kvp.Key] = boolVal;
                }
                else if (kvp.Value != null)
                {
                    metadata[kvp.Key] = kvp.Value.ToString() ?? "";
                }
            }
        }

        return metadata;
    }

    private static VectorDocument? ConvertToVectorDocument(Vector vector)
    {
        var metadata = new Dictionary<string, object>();
        string content = "";

        if (vector.Metadata != null)
        {
            foreach (var kvp in vector.Metadata)
            {
                if (kvp.Key == "content")
                {
                    content = kvp.Value.ToString() ?? "";
                }
                else
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }
        }

        return new VectorDocument(
            id: vector.Id,
            content: content,
            metadata: metadata.Count > 0 ? metadata : null,
            embedding: vector.Values.ToArray()
        );
    }

    private static VectorDocument? ConvertToVectorDocument(ScoredVector vector)
    {
        var metadata = new Dictionary<string, object>();
        string content = "";

        if (vector.Metadata != null)
        {
            foreach (var kvp in vector.Metadata)
            {
                if (kvp.Key == "content")
                {
                    content = kvp.Value.ToString() ?? "";
                }
                else
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }
        }

        return new VectorDocument(
            id: vector.Id,
            content: content,
            metadata: metadata.Count > 0 ? metadata : null,
            embedding: vector.Values?.ToArray(),
            score: vector.Score
        );
    }
}
