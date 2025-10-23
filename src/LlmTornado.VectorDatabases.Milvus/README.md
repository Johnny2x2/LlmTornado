# LlmTornado.VectorDatabases.Milvus

A Milvus vector database connector for LlmTornado, providing integration with the Milvus vector store.

## Overview

This package provides a connector to the Milvus vector database for use with LlmTornado. It implements the `IVectorDatabase` interface and uses the official Milvus.Client SDK to interact with Milvus.

## Installation

```bash
dotnet add package LlmTornado.VectorDatabases.Milvus
```

## Usage

```csharp
using LlmTornado.VectorDatabases.Milvus.Integrations;

// Create a Milvus vector database connection
var milvusDb = new MilvusVectorDatabase(
    host: "localhost",
    port: 19530,
    vectorDimension: 1536
);

// Initialize a collection
await milvusDb.InitializeCollection("my_collection");

// Add documents with embeddings
var documents = new[]
{
    new VectorDocument(
        id: "doc1",
        content: "Sample document",
        embedding: new float[1536], // Your embedding vector
        metadata: new Dictionary<string, object> { ["source"] = "example" }
    )
};

await milvusDb.AddDocumentsAsync(documents);

// Query by embedding
var queryEmbedding = new float[1536]; // Your query embedding
var results = await milvusDb.QueryByEmbeddingAsync(queryEmbedding, topK: 5);
```

## Features

- Full implementation of the `IVectorDatabase` interface
- Support for all CRUD operations on documents
- Similarity search using vector embeddings
- Metadata filtering support
- Collection management

## Requirements

- .NET 8.0 or .NET Standard 2.0
- Milvus 2.3+
- Milvus.Client 2.3.0-preview.1 or later

## License

See the main LlmTornado repository for license information.
