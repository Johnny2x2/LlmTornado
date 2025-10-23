using Pinecone;

namespace LlmTornado.VectorDatabases.Pinecone.Integrations;

/// <summary>
/// Helper class to convert TornadoWhereOperator to Pinecone Metadata filters.
/// </summary>
public class TornadoPineconeWhere
{
    private readonly TornadoWhereOperator? _tornadoWhereOperator;

    /// <summary>
    /// Initializes a new instance of the <see cref="TornadoPineconeWhere"/> class.
    /// </summary>
    /// <param name="where">The TornadoWhereOperator to convert.</param>
    public TornadoPineconeWhere(TornadoWhereOperator where)
    {
        _tornadoWhereOperator = where;
    }

    /// <summary>
    /// Converts the TornadoWhereOperator to Pinecone Metadata format.
    /// </summary>
    /// <returns>Pinecone Metadata representing the filter.</returns>
    public Metadata? ToMetadata()
    {
        if (_tornadoWhereOperator == null)
        {
            return null;
        }

        var whereDict = _tornadoWhereOperator.ToWhere();
        return ConvertDictionaryToMetadata(whereDict);
    }

    private static Metadata ConvertDictionaryToMetadata(Dictionary<string, object> dict)
    {
        var metadata = new Metadata();

        foreach (var kvp in dict)
        {
            var key = kvp.Key;
            var value = kvp.Value;

            // Handle logical operators ($and, $or) - for now, skip them as they require special handling in Pinecone
            // Pinecone may not support nested logical operators in the same way
            if (key == "$and" || key == "$or")
            {
                // Skip for now - complex logical operators need to be restructured
                continue;
            }
            // Handle field operators
            else if (value is Dictionary<string, object> operatorDict)
            {
                foreach (var op in operatorDict)
                {
                    var opKey = op.Key;
                    var opValue = op.Value;

                    // Map Tornado operators to Pinecone operators
                    var pineconeOp = MapOperator(opKey);
                    
                    var fieldMetadata = new Metadata();
                    fieldMetadata[pineconeOp] = ConvertToMetadataValue(opValue);
                    metadata[key] = fieldMetadata;
                }
            }
            // Direct equality
            else
            {
                metadata[key] = ConvertToMetadataValue(value);
            }
        }

        return metadata;
    }

    private static string MapOperator(string tornadoOp)
    {
        return tornadoOp switch
        {
            "$eq" => "$eq",
            "$ne" => "$ne",
            "$gt" => "$gt",
            "$gte" => "$gte",
            "$lt" => "$lt",
            "$lte" => "$lte",
            "$in" => "$in",
            "$nin" => "$nin",
            _ => tornadoOp
        };
    }

    private static MetadataValue ConvertToMetadataValue(object value)
    {
        if (value is string str)
        {
            return str;
        }
        if (value is int intVal)
        {
            return intVal;
        }
        if (value is long longVal)
        {
            return longVal;
        }
        if (value is float floatVal)
        {
            return floatVal;
        }
        if (value is double doubleVal)
        {
            return doubleVal;
        }
        if (value is bool boolVal)
        {
            return boolVal;
        }
        if (value is object[] array)
        {
            var metadataArray = new MetadataValue[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                metadataArray[i] = ConvertToMetadataValue(array[i]);
            }
            return metadataArray;
        }
        // Default: convert to string
        return value?.ToString() ?? "";
    }
}
