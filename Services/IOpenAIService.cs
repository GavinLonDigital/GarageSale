namespace GarageSale.Services;

public interface IOpenAIService
{
    Task<string?> GetResponse(string prompt);
}
