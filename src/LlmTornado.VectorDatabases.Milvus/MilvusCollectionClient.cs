using MC = Milvus.Client;
using System.Text.Json;

namespace LlmTornado.VectorDatabases.Milvus;

/// <summary>
/// Client for interacting with a specific Milvus collection.
/// Note: This is a simplified implementation that requires further development for full functionality.
/// </summary>
public class MilvusCollectionClient
{
    private readonly MilvusCollection _collection;
    private readonly MilvusClient _client;

    /// <summary>
    /// Initializes a new instance of the MilvusCollectionClient class.
    /// </summary>
    /// <param name="collection">The collection to interact with.</param>
    /// <param name="client">The Milvus client.</param>
    public MilvusCollectionClient(MilvusCollection collection, MilvusClient client)
    {
        _collection = collection;
        _client = client;
    }

    /// <summary>
    /// Gets the collection.
    /// </summary>
    public MilvusCollection Collection => _collection;

    /// <summary>
    /// Gets an entry by ID.
    /// </summary>
    /// <param name="id">The ID of the entry.</param>
    /// <returns>The entry if found, null otherwise.</returns>
    public async Task<MilvusEntry?> GetAsync(string id)
    {
        var entries = await GetAsync(new[] { id });
        return entries.FirstOrDefault();
    }

    /// <summary>
    /// Gets multiple entries by their IDs.
    /// </summary>
    /// <param name="ids">The IDs of the entries.</param>
    /// <returns>A list of found entries.</returns>
    public async Task<List<MilvusEntry>> GetAsync(string[] ids)
    {
        var internalClient = _client.GetInternalClient();
        
        // Build filter expression for the IDs
        var idList = string.Join(", ", ids.Select(id => $"\"{id}\""));
        var expr = $"id in [{idList}]";

        var queryResult = await internalClient.QueryAsync(
            _collection.Name,
            expr,
            new MC.QueryParameters
            {
                OutputFields = { "id", "document", "metadata" }
            }
        );

        var entries = new List<MilvusEntry>();
        if (queryResult.FieldsData.Any())
        {
            var idField = queryResult.FieldsData.FirstOrDefault(f => f.FieldName == "id");
            var documentField = queryResult.FieldsData.FirstOrDefault(f => f.FieldName == "document");
            var metadataField = queryResult.FieldsData.FirstOrDefault(f => f.FieldName == "metadata");

            if (idField != null)
            {
                var idData = idField.Data.Cast<string>().ToList();
                for (int i = 0; i < idData.Count; i++)
                {
                    var document = documentField?.Data.Cast<string>().ElementAtOrDefault(i);
                    var metadataJson = metadataField?.Data.Cast<string>().ElementAtOrDefault(i);
                    
                    Dictionary<string, object>? metadata = null;
                    if (!string.IsNullOrEmpty(metadataJson))
                    {
                        try
                        {
                            metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson);
                        }
                        catch
                        {
                            // Ignore deserialization errors
                        }
                    }

                    entries.Add(new MilvusEntry(idData[i], document, metadata));
                }
            }
        }

        return entries;
    }

    /// <summary>
    /// Performs a similarity search using a query embedding.
    /// </summary>
    /// <param name="queryEmbedding">The query embedding vector.</param>
    /// <param name="topK">The number of results to return.</param>
    /// <param name="whereMetadata">Optional metadata filters.</param>
    /// <returns>A list of matching entries with similarity scores.</returns>
    public async Task<List<MilvusEntry>> QueryAsync(float[] queryEmbedding, int topK = 10, Dictionary<string, object>? whereMetadata = null)
    {
        var internalClient = _client.GetInternalClient();

        // Build filter expression from metadata if provided
        string? filterExpr = null;
        if (whereMetadata != null && whereMetadata.Count > 0)
        {
            filterExpr = BuildMetadataFilter(whereMetadata);
        }

        var searchParams = new MC.SearchParameters
        {
            VectorFieldName = "embedding",
            Vectors = new ReadOnlyMemory<float>[] { new ReadOnlyMemory<float>(queryEmbedding) },
            Limit = topK,
            MetricType = MC.SimilarityMetricType.Cosine,
            OutputFields = { "id", "document", "metadata" }
        };

        if (!string.IsNullOrEmpty(filterExpr))
        {
            searchParams.Expression = filterExpr;
        }

        var searchResult = await internalClient.SearchAsync(
            _collection.Name,
            searchParams
        );

        var entries = new List<MilvusEntry>();
        
        if (searchResult.Results.Ids.Count > 0)
        {
            for (int i = 0; i < searchResult.Results.Ids.Count; i++)
            {
                var id = searchResult.Results.Ids.StringIds != null && searchResult.Results.Ids.StringIds.Count > i 
                    ? searchResult.Results.Ids.StringIds[i] 
                    : searchResult.Results.Ids.LongIds != null && searchResult.Results.Ids.LongIds.Count > i 
                        ? searchResult.Results.Ids.LongIds[i].ToString() 
                        : "";
                
                var distance = searchResult.Results.Scores != null && searchResult.Results.Scores.Count > i 
                    ? searchResult.Results.Scores[i] 
                    : 0f;
                
                string? document = null;
                Dictionary<string, object>? metadata = null;

                // Extract field data
                foreach (var fieldData in searchResult.Results.FieldsData)
                {
                    if (fieldData.FieldName == "document" && fieldData.Data.Cast<string>().Count() > i)
                    {
                        document = fieldData.Data.Cast<string>().ElementAtOrDefault(i);
                    }
                    else if (fieldData.FieldName == "metadata" && fieldData.Data.Cast<string>().Count() > i)
                    {
                        var metadataJson = fieldData.Data.Cast<string>().ElementAtOrDefault(i);
                        if (!string.IsNullOrEmpty(metadataJson))
                        {
                            try
                            {
                                metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson);
                            }
                            catch
                            {
                                // Ignore deserialization errors
                            }
                        }
                    }
                }

                entries.Add(new MilvusEntry(id, document, metadata, null, distance));
            }
        }

        return entries;
    }

    /// <summary>
    /// Adds new entries to the collection.
    /// </summary>
    /// <param name="ids">The IDs of the entries.</param>
    /// <param name="embeddings">The embeddings of the entries.</param>
    /// <param name="metadatas">The metadata of the entries.</param>
    /// <param name="documents">The document contents of the entries.</param>
    public async Task AddAsync(List<string> ids, List<float[]>? embeddings = null, List<Dictionary<string, object>>? metadatas = null, List<string>? documents = null)
    {
        if (ids.Count == 0) return;

        var internalClient = _client.GetInternalClient();

        // Prepare data for insertion
        var fieldData = new List<MC.FieldData>
        {
            new MC.FieldData
            {
                FieldName = "id",
                Data = ids
            },
            new MC.FieldData
            {
                FieldName = "document",
                Data = documents ?? Enumerable.Repeat("", ids.Count).ToList()
            },
            new MC.FieldData
            {
                FieldName = "metadata",
                Data = metadatas?.Select(m => JsonSerializer.Serialize(m)).ToList() ?? Enumerable.Repeat("{}", ids.Count).ToList()
            },
            new MC.FieldData
            {
                FieldName = "embedding",
                Data = embeddings ?? Enumerable.Repeat(new float[_collection.VectorDimension], ids.Count).ToList()
            }
        };

        await internalClient.InsertAsync(
            _collection.Name,
            fieldData
        );
    }

    /// <summary>
    /// Updates existing entries in the collection.
    /// </summary>
    /// <param name="ids">The IDs of the entries to update.</param>
    /// <param name="embeddings">The new embeddings.</param>
    /// <param name="metadatas">The new metadata.</param>
    /// <param name="documents">The new document contents.</param>
    public async Task UpdateAsync(List<string> ids, List<float[]>? embeddings = null, List<Dictionary<string, object>>? metadatas = null, List<string>? documents = null)
    {
        // Milvus doesn't have a direct update operation, so we delete and re-insert
        await DeleteAsync(ids);
        await AddAsync(ids, embeddings, metadatas, documents);
    }

    /// <summary>
    /// Upserts entries (insert or update) in the collection.
    /// </summary>
    /// <param name="ids">The IDs of the entries.</param>
    /// <param name="embeddings">The embeddings.</param>
    /// <param name="metadatas">The metadata.</param>
    /// <param name="documents">The document contents.</param>
    public async Task UpsertAsync(List<string> ids, List<float[]>? embeddings = null, List<Dictionary<string, object>>? metadatas = null, List<string>? documents = null)
    {
        if (ids.Count == 0) return;

        var internalClient = _client.GetInternalClient();

        // Prepare data for upsertion
        var fieldData = new List<MC.FieldData>
        {
            new MC.FieldData
            {
                FieldName = "id",
                Data = ids
            },
            new MC.FieldData
            {
                FieldName = "document",
                Data = documents ?? Enumerable.Repeat("", ids.Count).ToList()
            },
            new MC.FieldData
            {
                FieldName = "metadata",
                Data = metadatas?.Select(m => JsonSerializer.Serialize(m)).ToList() ?? Enumerable.Repeat("{}", ids.Count).ToList()
            },
            new MC.FieldData
            {
                FieldName = "embedding",
                Data = embeddings ?? Enumerable.Repeat(new float[_collection.VectorDimension], ids.Count).ToList()
            }
        };

        await internalClient.UpsertAsync(
            _collection.Name,
            fieldData
        );
    }

    /// <summary>
    /// Deletes entries by their IDs.
    /// </summary>
    /// <param name="ids">The IDs of the entries to delete.</param>
    public async Task DeleteAsync(List<string> ids)
    {
        if (ids.Count == 0) return;

        var internalClient = _client.GetInternalClient();
        
        // Build filter expression for deletion
        var idList = string.Join(", ", ids.Select(id => $"\"{id}\""));
        var expr = $"id in [{idList}]";

        await internalClient.DeleteAsync(_collection.Name, expr);
    }

    /// <summary>
    /// Counts the number of entries in the collection.
    /// </summary>
    /// <returns>The count of entries.</returns>
    public async Task<long> CountAsync()
    {
        var internalClient = _client.GetInternalClient();
        var stats = await internalClient.GetCollectionStatisticsAsync(_collection.Name);
        
        // Parse row count from statistics
        if (stats.Stats.TryGetValue("row_count", out var rowCountStr) && long.TryParse(rowCountStr, out var rowCount))
        {
            return rowCount;
        }

        return 0;
    }

    /// <summary>
    /// Builds a metadata filter expression for Milvus.
    /// </summary>
    /// <param name="whereMetadata">The metadata filters.</param>
    /// <returns>A filter expression string.</returns>
    private static string BuildMetadataFilter(Dictionary<string, object> whereMetadata)
    {
        var conditions = new List<string>();

        foreach (var kvp in whereMetadata)
        {
            var key = kvp.Key;
            var value = kvp.Value;

            if (value is Dictionary<string, object> operatorDict)
            {
                foreach (var op in operatorDict)
                {
                    var condition = BuildCondition(key, op.Key, op.Value);
                    if (condition != null)
                    {
                        conditions.Add(condition);
                    }
                }
            }
            else
            {
                // Direct equality - use JSON containment
                var jsonValue = JsonSerializer.Serialize(value);
                conditions.Add($"json_contains(metadata, '\"{key}\":{jsonValue}')");
            }
        }

        return string.Join(" && ", conditions);
    }

    /// <summary>
    /// Builds a single condition for the filter expression.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="op">The operator.</param>
    /// <param name="value">The value.</param>
    /// <returns>A condition string.</returns>
    private static string? BuildCondition(string key, string op, object value)
    {
        var jsonValue = JsonSerializer.Serialize(value);
        
        return op switch
        {
            "$eq" => $"json_contains(metadata, '\"{key}\":{jsonValue}')",
            "$ne" => $"!json_contains(metadata, '\"{key}\":{jsonValue}')",
            "$gt" => $"json_extract(metadata, '$.{key}') > {value}",
            "$gte" => $"json_extract(metadata, '$.{key}') >= {value}",
            "$lt" => $"json_extract(metadata, '$.{key}') < {value}",
            "$lte" => $"json_extract(metadata, '$.{key}') <= {value}",
            "$in" => value is Array arr
                ? $"json_extract(metadata, '$.{key}') in [{string.Join(", ", arr.Cast<object>().Select(v => JsonSerializer.Serialize(v)))}]"
                : null,
            "$nin" => value is Array arr2
                ? $"json_extract(metadata, '$.{key}') not in [{string.Join(", ", arr2.Cast<object>().Select(v => JsonSerializer.Serialize(v)))}]"
                : null,
            _ => null
        };
    }
}
