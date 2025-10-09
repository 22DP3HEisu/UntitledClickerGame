using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Clean and maintainable API client for Unity with authentication support.
/// Provides simple async HTTP methods with automatic token handling and refresh logic.
/// </summary>
public static class ApiClient
{
    public static string BaseUrl = "http://92.5.105.149:3000";
    
    /// <summary>
    /// Optional callback for token refresh. Should return true if refresh succeeded.
    /// </summary>
    public static Func<Task<bool>> OnTokenRefresh;
    
    /// <summary>
    /// Optional callback when token expires and cannot be refreshed.
    /// </summary>
    public static Action OnTokenExpired;

    // Public API Methods
    public static Task<TResponse> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default)
        => SendRequestAsync<TResponse>("GET", path, null, cancellationToken);

    public static Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
        => SendRequestAsync<TResponse>("POST", path, body, cancellationToken);

    public static Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
        => SendRequestAsync<TResponse>("PUT", path, body, cancellationToken);

    public static Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        => SendRequestAsync<object>("DELETE", path, null, cancellationToken);

    // Token Management
    public static void SetAuthToken(string token, DateTime? expiryUtc = null)
    {
        AuthTokenManager.SetToken(token, expiryUtc);
    }

    public static void ClearAuthToken()
    {
        AuthTokenManager.ClearToken();
    }

    public static bool IsTokenValid()
    {
        return AuthTokenManager.IsTokenValid();
    }

    // Core Request Logic
    private static async Task<TResponse> SendRequestAsync<TResponse>(string method, string path, object body, CancellationToken cancellationToken)
    {
        const int maxRetries = 1;
        bool hasAttemptedRefresh = false;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                using (var request = CreateRequest(method, path, body))
                {
                    var response = await ExecuteRequestAsync<TResponse>(request, cancellationToken);
                    return response;
                }
            }
            catch (ApiException ex) when (ex.StatusCode == 401 && !hasAttemptedRefresh && OnTokenRefresh != null)
            {
                hasAttemptedRefresh = true;
                
                try
                {
                    bool refreshSuccess = await OnTokenRefresh();
                    if (refreshSuccess)
                    {
                        continue; // Retry the request
                    }
                }
                catch (Exception refreshEx)
                {
                    Debug.LogWarning($"Token refresh failed: {refreshEx.Message}");
                }

                // Refresh failed, clear token and notify
                AuthTokenManager.ClearToken();
                OnTokenExpired?.Invoke();
                throw;
            }
        }

        throw new InvalidOperationException("Should not reach here");
    }

    private static UnityWebRequest CreateRequest(string method, string path, object body)
    {
        string url = BuildUrl(path);
        UnityWebRequest request;

        if (method == "GET")
        {
            request = UnityWebRequest.Get(url);
        }
        else
        {
            request = new UnityWebRequest(url, method);
            request.downloadHandler = new DownloadHandlerBuffer();

            if (body != null)
            {
                string jsonData = JsonUtility.ToJson(body);
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", "application/json");
            }
        }

        // Add auth header if token is valid
        if (AuthTokenManager.IsTokenValid())
        {
            string token = AuthTokenManager.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            }
        }

        return request;
    }

    private static async Task<TResponse> ExecuteRequestAsync<TResponse>(UnityWebRequest request, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<TResponse>();
        
        // Handle cancellation
        cancellationToken.Register(() =>
        {
            if (!request.isDone)
            {
                request.Abort();
            }
            tcs.TrySetCanceled();
        });

        var operation = request.SendWebRequest();
        
        operation.completed += _ =>
        {
            try
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    if (typeof(TResponse) == typeof(object))
                    {
                        tcs.TrySetResult(default(TResponse)); // For DELETE requests that don't return data
                    }
                    else
                    {
                        var response = JsonUtility.FromJson<TResponse>(request.downloadHandler.text);
                        tcs.TrySetResult(response);
                    }
                }
                else
                {
                    var apiException = CreateApiException(request);
                    tcs.TrySetException(apiException);
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        };

        return await tcs.Task;
    }

    private static string BuildUrl(string path)
    {
        if (string.IsNullOrEmpty(path)) return BaseUrl;
        if (path.StartsWith("http://") || path.StartsWith("https://")) return path;
        if (path.StartsWith("/")) return BaseUrl.TrimEnd('/') + path;
        return BaseUrl.TrimEnd('/') + "/" + path;
    }

    private static ApiException CreateApiException(UnityWebRequest request)
    {
        string message = "Unknown error";
        
        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            message = "Cannot connect to server. Please check your internet connection.";
        }
        else
        {
            try
            {
                var errorResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler?.text ?? "");
                message = errorResponse?.message ?? errorResponse?.error ?? $"Server error: {request.responseCode}";
            }
            catch
            {
                message = $"Server error: {request.responseCode}";
            }
        }

        return new ApiException(message, (int)request.responseCode);
    }

    // Helper Classes
    [Serializable]
    private class ErrorResponse
    {
        public string error;
        public string message;
    }
}

/// <summary>
/// Handles authentication token storage and validation.
/// </summary>
public static class AuthTokenManager
{
    private const string TokenKey = "AuthToken";
    private const string ExpiryKey = "TokenExpiry";

    public static void SetToken(string token, DateTime? expiryUtc = null)
    {
        ExecuteOnMainThread(() =>
        {
            if (!string.IsNullOrEmpty(token))
            {
                PlayerPrefs.SetString(TokenKey, token);
                if (expiryUtc.HasValue)
                {
                    PlayerPrefs.SetString(ExpiryKey, expiryUtc.Value.ToBinary().ToString());
                }
            }
            else
            {
                ClearTokenInternal();
            }
            PlayerPrefs.Save();
        });
    }

    public static void ClearToken()
    {
        ExecuteOnMainThread(() =>
        {
            ClearTokenInternal();
            PlayerPrefs.Save();
        });
    }

    public static string GetToken()
    {
        return PlayerPrefs.GetString(TokenKey, "");
    }

    public static bool IsTokenValid()
    {
        string token = GetToken();
        if (string.IsNullOrEmpty(token)) return false;

        var expiry = GetTokenExpiry();
        if (!expiry.HasValue) return true; // No expiry set

        return DateTime.UtcNow < expiry.Value;
    }

    private static DateTime? GetTokenExpiry()
    {
        string raw = PlayerPrefs.GetString(ExpiryKey, "");
        if (string.IsNullOrEmpty(raw)) return null;

        try
        {
            long binary = Convert.ToInt64(raw);
            return DateTime.FromBinary(binary).ToUniversalTime();
        }
        catch
        {
            return null;
        }
    }

    private static void ClearTokenInternal()
    {
        PlayerPrefs.DeleteKey(TokenKey);
        PlayerPrefs.DeleteKey(ExpiryKey);
    }

    private static void ExecuteOnMainThread(System.Action action)
    {
        try
        {
            action();
        }
        catch (UnityException ex) when (ex.Message.Contains("main thread"))
        {
            // Marshal to main thread if needed
            UnityMainThreadDispatcher.Enqueue(action);
        }
    }
}

/// <summary>
/// Exception thrown by API operations.
/// </summary>
public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(string message, int statusCode = 0) : base(message)
    {
        StatusCode = statusCode;
    }
}
