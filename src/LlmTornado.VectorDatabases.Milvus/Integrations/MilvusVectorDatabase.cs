namespace LlmTornado.VectorDatabases.Milvus.Integrations;

/// <summary>
/// Milvus implementation of the IVectorDatabase interface.
/// </summary>
public class MilvusVectorDatabase : IVectorDatabase
{
    /// <summary>
    /// Gets the Milvus client.
    /// </summary>
    public MilvusClient MilvusClient { get; set; }

    /// <summary>
    /// Gets the current Milvus collection.
    /// </summary>
    public MilvusCollection? MilvusCollection { get; set; }

    /// <summary>
    /// Gets the collection client.
    /// </summary>
    public MilvusCollectionClient? CollectionClient { get; set; }

    /// <summary>
    /// Gets or sets the collection name.
    /// </summary>
    public string CollectionName { get; set; } = "defaultCollection";

    private MilvusConfigurationOptions _configOptions { get; set; }
    private int _vectorDimension { get; set; }

    /// <summary>
    /// Initializes a new instance of the MilvusVectorDatabase class.
    /// </summary>
    /// <param name="host">The Milvus server host.</param>
    /// <param name="port">The Milvus server port.</param>
    /// <param name="vectorDimension">The dimension of vectors. Default is 1536.</param>
    /// <param name="database">The database name.</param>
    /// <param name="username">The username for authentication.</param>
    /// <param name="password">The password for authentication.</param>
    /// <param name="useSsl">Whether to use SSL/TLS.</param>
    public MilvusVectorDatabase(
        string host,
        int port = 19530,
        int vectorDimension = 1536,
        string? database = null,
        string? username = null,
        string? password = null,
        bool useSsl = false)
    {
        _vectorDimension = vectorDimension;
        _configOptions = new MilvusConfigurationOptions(host, port, database, username, password, useSsl);
        MilvusClient = new MilvusClient(_configOptions);
        Task.Run(async () => await TestMilvusConnection()).Wait();
    }

    /// <summary>
    /// Initializes a new instance of the MilvusVectorDatabase class.
    /// </summary>
    /// <param name="options">Configuration options for the Milvus connection.</param>
    /// <param name="vectorDimension">The dimension of vectors. Default is 1536.</param>
    public MilvusVectorDatabase(MilvusConfigurationOptions options, int vectorDimension = 1536)
    {
        _vectorDimension = vectorDimension;
        _configOptions = options;
        MilvusClient = new MilvusClient(_configOptions);
        Task.Run(async () => await TestMilvusConnection()).Wait();
    }

    private async Task TestMilvusConnection()
    {
        try
        {
            // Test connection by listing collections
            await MilvusClient.ListCollectionsAsync();

            // Create and delete a test collection to verify functionality
            string testCollectionName = $"test_collection_{Guid.NewGuid().ToString().Substring(0, 4)}";
            await InitializeCollection(testCollectionName);
            await DeleteCollectionAsync(testCollectionName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Milvus instance not reachable or connection failed", ex);
        }
    }

    /// <summary>
    /// Initializes a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    public async Task InitializeCollection(string collectionName)
    {
        if (collectionName.Equals(CollectionName) && CollectionClient != null)
        {
            return;
        }

        CollectionName = collectionName;
        MilvusCollection = await MilvusClient.GetOrCreateCollectionAsync(collectionName, _vectorDimension);
        CollectionClient = new MilvusCollectionClient(MilvusCollection, MilvusClient);
    }

    /// <summary>
    /// Deletes a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection to delete.</param>
    public void DeleteCollection(string collectionName)
    {
        Task.Run(async () => await DeleteCollectionAsync(collectionName)).Wait();
    }

    /// <summary>
    /// Deletes a collection asynchronously.
    /// </summary>
    /// <param name="collectionName">The name of the collection to delete.</param>
    public async Task DeleteCollectionAsync(string collectionName)
    {
        await MilvusClient.DeleteCollectionAsync(collectionName);
        if (CollectionName == collectionName)
        {
            CollectionClient = null;
            MilvusCollection = null;
        }
    }

    private void ThrowIfCollectionNotInitialized()
    {
        if (CollectionClient == null)
        {
            throw new InvalidOperationException("CollectionClient is not initialized. Please initialize the collection first.");
        }
    }

    /// <summary>
    /// Gets the collection name.
    /// </summary>
    /// <returns>The collection name.</returns>
    public string GetCollectionName() => CollectionName;

    /// <summary>
    /// Adds documents to the vector database.
    /// </summary>
    /// <param name="documents">The documents to add.</param>
    public void AddDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await AddDocumentsAsync(documents)).Wait();
    }

    /// <summary>
    /// Adds documents to the vector database asynchronously.
    /// </summary>
    /// <param name="documents">The documents to add.</param>
    public async Task AddDocumentsAsync(VectorDocument[] documents)
    {
        ThrowIfCollectionNotInitialized();

        List<string> ids = new List<string>();
        List<float[]> embeddings = new List<float[]>();
        List<Dictionary<string, object>> metadatas = new List<Dictionary<string, object>>();
        List<string> contents = new List<string>();

        foreach (var doc in documents)
        {
            ids.Add(doc.Id);
            embeddings.Add(doc.Embedding ?? Array.Empty<float>());
            metadatas.Add(doc.Metadata ?? new Dictionary<string, object>());
            contents.Add(doc.Content ?? "");
        }

        await CollectionClient!.AddAsync(ids, embeddings: embeddings, metadatas: metadatas, documents: contents);
    }

    /// <summary>
    /// Gets documents by their IDs.
    /// </summary>
    /// <param name="ids">The IDs of the documents to retrieve.</param>
    /// <returns>An array of retrieved documents.</returns>
    public VectorDocument[]? GetDocuments(string[] ids)
    {
        return Task.Run(async () => await GetDocumentsAsync(ids)).Result;
    }

    /// <summary>
    /// Gets documents by their IDs asynchronously.
    /// </summary>
    /// <param name="ids">The IDs of the documents to retrieve.</param>
    /// <returns>An array of retrieved documents.</returns>
    public async Task<VectorDocument[]> GetDocumentsAsync(string[] ids)
    {
        ThrowIfCollectionNotInitialized();
        var entries = await CollectionClient!.GetAsync(ids);
        return entries.Select(e => new VectorDocument(
            e.Id,
            e.Document ?? "",
            e.Metadata,
            e.Embedding ?? Array.Empty<float>()
        )).ToArray();
    }

    /// <summary>
    /// Updates documents in the vector database.
    /// </summary>
    /// <param name="documents">The documents to update.</param>
    public void UpdateDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await UpdateDocumentsAsync(documents)).Wait();
    }

    /// <summary>
    /// Updates documents in the vector database asynchronously.
    /// </summary>
    /// <param name="documents">The documents to update.</param>
    public async Task UpdateDocumentsAsync(VectorDocument[] documents)
    {
        ThrowIfCollectionNotInitialized();
        await CollectionClient!.UpdateAsync(
            documents.Select(d => d.Id).ToList(),
            embeddings: documents.Select(d => d.Embedding ?? Array.Empty<float>()).ToList(),
            metadatas: documents.Select(d => d.Metadata ?? new Dictionary<string, object>()).ToList(),
            documents: documents.Select(d => d.Content ?? "").ToList()
        );
    }

    /// <summary>
    /// Upserts documents in the vector database.
    /// </summary>
    /// <param name="documents">The documents to upsert.</param>
    public void UpsertDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await UpsertDocumentsAsync(documents)).Wait();
    }

    /// <summary>
    /// Upserts documents in the vector database asynchronously.
    /// </summary>
    /// <param name="documents">The documents to upsert.</param>
    public async Task UpsertDocumentsAsync(VectorDocument[] documents)
    {
        ThrowIfCollectionNotInitialized();
        await CollectionClient!.UpsertAsync(
            documents.Select(d => d.Id).ToList(),
            embeddings: documents.Select(d => d.Embedding ?? Array.Empty<float>()).ToList(),
            metadatas: documents.Select(d => d.Metadata ?? new Dictionary<string, object>()).ToList(),
            documents: documents.Select(d => d.Content ?? "").ToList()
        );
    }

    /// <summary>
    /// Deletes documents by their IDs.
    /// </summary>
    /// <param name="ids">The IDs of the documents to delete.</param>
    public void DeleteDocuments(string[] ids)
    {
        Task.Run(async () => await DeleteDocumentsAsync(ids)).Wait();
    }

    /// <summary>
    /// Deletes documents by their IDs asynchronously.
    /// </summary>
    /// <param name="ids">The IDs of the documents to delete.</param>
    public async Task DeleteDocumentsAsync(string[] ids)
    {
        ThrowIfCollectionNotInitialized();
        await CollectionClient!.DeleteAsync(ids.ToList());
    }

    /// <summary>
    /// Queries the vector database using an embedding vector.
    /// </summary>
    /// <param name="embedding">The query embedding vector.</param>
    /// <param name="where">Optional metadata filters.</param>
    /// <param name="topK">The number of results to return.</param>
    /// <param name="includeScore">Whether to include similarity scores.</param>
    /// <returns>An array of matching documents.</returns>
    public VectorDocument[] QueryByEmbedding(float[] embedding, TornadoWhereOperator? where = null, int topK = 5, bool includeScore = false)
    {
        return Task.Run(async () => await QueryByEmbeddingAsync(embedding, where, topK, includeScore)).Result;
    }

    /// <summary>
    /// Queries the vector database using an embedding vector asynchronously.
    /// </summary>
    /// <param name="embedding">The query embedding vector.</param>
    /// <param name="where">Optional metadata filters.</param>
    /// <param name="topK">The number of results to return.</param>
    /// <param name="includeScore">Whether to include similarity scores.</param>
    /// <returns>An array of matching documents.</returns>
    public async Task<VectorDocument[]> QueryByEmbeddingAsync(float[] embedding, TornadoWhereOperator? where = null, int topK = 5, bool includeScore = false)
    {
        ThrowIfCollectionNotInitialized();

        Dictionary<string, object>? whereDict = null;
        if (where != null)
        {
            var tornadoMilvusWhere = new TornadoMilvusWhere(where);
            whereDict = tornadoMilvusWhere.ToWhere();
        }

        var entries = await CollectionClient!.QueryAsync(embedding, topK, whereDict);

        List<VectorDocument> results = new List<VectorDocument>();
        foreach (var entry in entries)
        {
            float[]? entryEmbedding = entry.Embedding ?? Array.Empty<float>();
            results.Add(new VectorDocument(
                entry.Id,
                entry.Document ?? "",
                entry.Metadata,
                entryEmbedding,
                entry.Distance
            ));
        }

        return results.ToArray();
    }
}
