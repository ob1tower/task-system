using System.Text;
using System.Text.Json;
using TaskSystemClient.Models;
using TaskSystemClient.Models.User;

namespace TaskSystemClient.Services;

public interface IAuthService : IDisposable
{
    Task<bool> RegisterAsync(string userName, string email, string password);
    Task<string?> LoginAsync(string email, string password);
    Task<bool> IsAuthenticatedAsync();
    string? GetAccessToken();
    void SetAccessToken(string? token);
}

public class AuthService : IAuthService
{
    private readonly string _tokenFilePath;
    private string? _accessToken;

    public AuthService()
    {
        // Path for storing the token locally
        _tokenFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                      "TaskSystemClient", "token.txt");

        // Try to load token from file storage
        LoadTokenFromFile();
    }

    private void LoadTokenFromFile()
    {
        try
        {
            if (File.Exists(_tokenFilePath))
            {
                var token = File.ReadAllText(_tokenFilePath);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    _accessToken = token.Trim();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading token from file: {ex.Message}");
        }
    }

    private void SaveTokenToFile(string? token)
    {
        try
        {
            var directory = Path.GetDirectoryName(_tokenFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (string.IsNullOrEmpty(token))
            {
                if (File.Exists(_tokenFilePath))
                {
                    File.Delete(_tokenFilePath);
                }
            }
            else
            {
                File.WriteAllText(_tokenFilePath, token);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving token to file: {ex.Message}");
        }
    }

    public async Task<bool> RegisterAsync(string userName, string email, string password)
    {
        // For RabbitMQ-based authentication, this function is no longer used for sending HTTP requests
        // But we keep it for backward compatibility or possible future usage
        Console.WriteLine("Registration through HTTP is not implemented in this version. Use RabbitMQ messaging instead.");
        return false;
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        // For RabbitMQ-based authentication, this function is no longer used for sending HTTP requests
        // But we keep it for backward compatibility or possible future usage
        Console.WriteLine("Login through HTTP is not implemented in this version. Use RabbitMQ messaging instead.");
        return null;
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        return Task.FromResult(!string.IsNullOrEmpty(_accessToken));
    }

    public string? GetAccessToken()
    {
        return _accessToken;
    }

    public void SetAccessToken(string? token)
    {
        _accessToken = token;
        SaveTokenToFile(token);
    }

    public void Dispose()
    {
        // HttpClient is no longer used, so there's no need to dispose it
    }
}