namespace LlmTornado.VectorDatabases.Milvus;

/// <summary>
/// Configuration options for connecting to a Milvus vector database.
/// </summary>
public class MilvusConfigurationOptions
{
    /// <summary>
    /// The host address of the Milvus server.
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// The port of the Milvus server. Default is 19530.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// The database name to use. If not specified, the default database is used.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// The username for authentication (if required).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// The password for authentication (if required).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Whether to use SSL/TLS for the connection.
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// Initializes a new instance of the MilvusConfigurationOptions class.
    /// </summary>
    /// <param name="host">The host address of the Milvus server.</param>
    /// <param name="port">The port of the Milvus server. Default is 19530.</param>
    /// <param name="database">The database name to use.</param>
    /// <param name="username">The username for authentication.</param>
    /// <param name="password">The password for authentication.</param>
    /// <param name="useSsl">Whether to use SSL/TLS for the connection.</param>
    public MilvusConfigurationOptions(
        string host, 
        int port = 19530, 
        string? database = null,
        string? username = null,
        string? password = null,
        bool useSsl = false)
    {
        Host = host;
        Port = port;
        Database = database;
        Username = username;
        Password = password;
        UseSsl = useSsl;
    }
}
