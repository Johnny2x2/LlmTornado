# LlmTornado.VectorDatabases.Pinecone

Pinecone vector database integration for LlmTornado.

## Installation

```bash
dotnet add package LlmTornado.VectorDatabases.Pinecone
```

## Usage

```csharp
using LlmTornado.VectorDatabases;
using LlmTornado.VectorDatabases.Pinecone.Integrations;

// Initialize Pinecone client
var pinecone = new TornadoPinecone(
    apiKey: "your-pinecone-api-key",
    vectorDimension: 1536
);

// Initialize a collection (index)
await pinecone.InitializeCollectionAsync("my-index", "my-namespace");

// Add documents
var documents = new[]
{
    new VectorDocument(
        id: "doc1",
        content: "This is a sample document",
        metadata: new Dictionary<string, object> { ["category"] = "sample" },
        embedding: new float[] { 0.1f, 0.2f, 0.3f, ... }
    )
};

await pinecone.AddDocumentsAsync(documents);

// Query by embedding
var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f, ... };
var results = await pinecone.QueryByEmbeddingAsync(
    embedding: queryEmbedding,
    topK: 5,
    includeScore: true
);

// Get documents by ID
var retrieved = await pinecone.GetDocumentsAsync(new[] { "doc1" });

// Delete documents
await pinecone.DeleteDocumentsAsync(new[] { "doc1" });

// Delete collection
await pinecone.DeleteCollectionAsync("my-index");
```

## Features

- ✅ Create/delete Pinecone indexes (collections)
- ✅ Add/upsert/update/delete documents
- ✅ Query by vector embedding
- ✅ Retrieve documents by ID
- ✅ Metadata filtering support
- ✅ Async operations
- ✅ Built on official Pinecone .NET SDK

## Requirements

- .NET 8.0+ or .NET Standard 2.0+
- Pinecone API key

## License

MIT
