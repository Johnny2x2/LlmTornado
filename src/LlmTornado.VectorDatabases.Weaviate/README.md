# LlmTornado.VectorDatabases.Weaviate

C# connector for Weaviate vector database, implementing the `IVectorDatabase` interface for LlmTornado.

## Overview

This package provides integration between LlmTornado and Weaviate, a cloud-native vector database. It uses the official tryAGI/Weaviate .NET SDK to communicate with Weaviate instances.

## Installation

```bash
dotnet add package LlmTornado.VectorDatabases.Weaviate
```

## Usage

```csharp
using LlmTornado.VectorDatabases.Weaviate.Integrations;

// Initialize the Weaviate connector
var weaviateDb = new WeaviateVectorDatabase(
    uri: "http://localhost:8080",
    vectorDimension: 1536,
    apiKey: "your-api-key" // optional
);

// Initialize a collection
await weaviateDb.InitializeCollection("my_documents");

// Add documents with embeddings
var documents = new[]
{
    new VectorDocument(
        id: Guid.NewGuid().ToString(),
        content: "Hello world",
        embedding: new float[1536] { /* ... */ },
        metadata: new Dictionary<string, object> 
        { 
            { "source", "test" },
            { "timestamp", DateTime.UtcNow }
        }
    )
};

await weaviateDb.AddDocumentsAsync(documents);

// Query by embedding
var queryEmbedding = new float[1536] { /* ... */ };
var results = await weaviateDb.QueryByEmbeddingAsync(
    embedding: queryEmbedding,
    topK: 5,
    includeScore: true
);
```

## Features

- ✅ Create and delete collections
- ✅ Add, update, upsert, and delete documents
- ✅ Retrieve documents by ID
- ⚠️ Query by embedding vector (basic implementation)
- ⚠️ Metadata filtering with TornadoWhereOperator (to be implemented)

## Dependencies

- **Weaviate SDK**: tryAGI/Weaviate (v0.0.0-dev.38)
- **LlmTornado.VectorDatabases**: Base interface and types

## Configuration

### Connection String
The `uri` parameter should point to your Weaviate instance:
- Local: `http://localhost:8080`
- Cloud: `https://your-instance.weaviate.network`

### Authentication
For authenticated instances, provide the API key:
```csharp
var db = new WeaviateVectorDatabase(
    uri: "https://your-instance.weaviate.network",
    vectorDimension: 1536,
    apiKey: "your-weaviate-api-key"
);
```

## Implementation Notes

### Vector Dimensions
The `vectorDimension` parameter must match the dimension of embeddings you plan to store. Common values:
- OpenAI ada-002: 1536
- Other models: varies (check your embedding model documentation)

### Document IDs
Weaviate uses GUIDs for object IDs. The connector will attempt to parse your string IDs as GUIDs or generate new ones if needed.

### Query Implementation
The current implementation includes basic structure for vector similarity search. Full GraphQL query support with metadata filtering is planned for future releases.

##Status

This is a working implementation that satisfies the IVectorDatabase interface. Some advanced features like complex metadata filtering and GraphQL-based vector search are marked for future enhancement.

## License

This project follows the same license as LlmTornado.
