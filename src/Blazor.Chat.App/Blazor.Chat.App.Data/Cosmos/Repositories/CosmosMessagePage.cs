namespace Blazor.Chat.App.Data.Cosmos.Repositories;

/// <summary>
/// Paginated result for Cosmos message queries
/// </summary>
public record CosmosMessagePage
{
    public List<CosmosMessageDocument> Messages { get; init; } = new();
    public string? ContinuationToken { get; init; }
    public bool HasMoreResults { get; init; }
    public int Count { get; init; }
}