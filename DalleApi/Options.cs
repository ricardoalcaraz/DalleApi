using System.ComponentModel.DataAnnotations;

namespace DalleApi;

public record RedisOptions
{
    [Required] public string? ConnectionString { get; init; } = "localhost:6379";
    
    [Required] public string? StreamName { get; init; } = "dalle_stream";
    [Required] public string? TextPromptStream { get; init; } = "text_prompt";
    [Required] public string? TestStream { get; init; } = "test";
}