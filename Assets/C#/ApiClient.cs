using System;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Static API client that provides Task-based async methods for HTTP requests.
/// Use ApiClient.PostAsync / ApiClient.GetAsync. These wrappers run UnityWebRequest
/// and return Task&lt;TResponse&gt; suitable for async/await usage.
/// </summary>
public static class ApiClient
{
    public static string BaseUrl = "http://92.5.105.149:3000";
    /// <summary>
    /// Optional async function that will be called to attempt refreshing the auth token when a request receives 401.
    /// Should return true if refresh succeeded and a new token was stored (via SetAuthToken).
    /// Application code should assign this at startup if refresh is supported by backend.
    /// </summary>
    public static Func<System.Threading.Tasks.Task<bool>> RefreshTokenAsync;

    /// <summary>
    /// Optional callback invoked when token is determined to be expired/invalid and refresh failed.
    /// Use this to navigate to login UI or clear sensitive state.
    /// </summary>
    public static Action OnTokenExpired;


    // URL builder used by async wrappers
    internal static string BuildUrl(string path)
    {
        if (string.IsNullOrEmpty(path)) return BaseUrl;
        if (path.StartsWith("http://") || path.StartsWith("https://")) return path;
        if (path.StartsWith("/")) return BaseUrl.TrimEnd('/') + path;
        return BaseUrl.TrimEnd('/') + "/" + path;
    }

    internal static string ParseRequestError(UnityWebRequest request)
    {
        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError($"ApiClient: Connection error: {request.error}");
            return "Cannot connect to server. Please check your internet connection.";
        }

        try
        {
            var text = request.downloadHandler?.text ?? "";
            var err = JsonUtility.FromJson<ErrorResponse>(text);
            if (!string.IsNullOrEmpty(err.message)) return err.message;
            if (!string.IsNullOrEmpty(err.error)) return err.error;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ApiClient: Failed to parse error body: {e.Message}");
        }

        return $"Server error: {request.responseCode}";
    }

    /// <summary>
    /// Save or clear the auth token. Optionally persist an expiry time (UTC binary string).
    /// If expiry is null the token will be saved without an expiry (caller assumes responsibility).
    /// </summary>
    public static void SetAuthToken(string token, DateTime? expiryUtc = null)
    {
        if (!string.IsNullOrEmpty(token))
        {
            PlayerPrefs.SetString("AuthToken", token);
            if (expiryUtc.HasValue)
            {
                PlayerPrefs.SetString("TokenExpiry", expiryUtc.Value.ToBinary().ToString());
            }
        }
        else
        {
            PlayerPrefs.DeleteKey("AuthToken");
            PlayerPrefs.DeleteKey("TokenExpiry");
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Clear stored auth token and expiry.
    /// </summary>
    public static void ClearAuthToken()
    {
        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.DeleteKey("TokenExpiry");
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Get the stored token expiry in UTC if present. Returns null if missing or unparsable.
    /// </summary>
    public static DateTime? GetTokenExpiry()
    {
        string raw = PlayerPrefs.GetString("TokenExpiry", "");
        if (string.IsNullOrEmpty(raw)) return null;

        try
        {
            long bin = Convert.ToInt64(raw);
            DateTime dt = DateTime.FromBinary(bin);
            return dt.ToUniversalTime();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns true if an auth token exists and the expiry (if set) is still in the future.
    /// If no expiry is set the token is considered valid (caller responsibility to refresh when needed).
    /// </summary>
    public static bool IsTokenValid()
    {
        string token = PlayerPrefs.GetString("AuthToken", "");
        if (string.IsNullOrEmpty(token)) return false;

        var expiry = GetTokenExpiry();
        if (!expiry.HasValue) return true;

        // Compare in UTC
        return DateTime.UtcNow < expiry.Value;
    }

    [Serializable]
    private class ErrorResponse
    {
        public string error;
        public string message;
    }

    public class ApiUnauthorizedException : Exception
    {
        public ApiUnauthorizedException(string message) : base(message) { }
    }

    // -------------------- Async wrappers --------------------
    public static System.Threading.Tasks.Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, System.Threading.CancellationToken cancellationToken = default)
    {
        return SendWithRefreshAsync(async () =>
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<TResponse>(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);

            string url = BuildUrl(path);
            string jsonData = JsonUtility.ToJson(body);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

            UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Only attach token if present and not expired
            if (IsTokenValid())
            {
                string token = PlayerPrefs.GetString("AuthToken", "");
                if (!string.IsNullOrEmpty(token))
                    request.SetRequestHeader("Authorization", $"Bearer {token}");
            }

            var operation = request.SendWebRequest();

            if (cancellationToken != default)
            {
                cancellationToken.Register(() =>
                {
                    if (!operation.isDone)
                        request.Abort();
                    tcs.TrySetCanceled();
                });
            }

            operation.completed += _ =>
            {
                try
                {
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var resp = JsonUtility.FromJson<TResponse>(request.downloadHandler.text);
                        tcs.TrySetResult(resp);
                    }
                    else
                    {
                        var msg = ParseRequestError(request);
                        tcs.TrySetException(new Exception(msg));
                    }
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                finally
                {
                    request.Dispose();
                }
            };

            return await tcs.Task.ConfigureAwait(false);
        }, cancellationToken);
    }

    public static System.Threading.Tasks.Task<TResponse> GetAsync<TResponse>(string path, System.Threading.CancellationToken cancellationToken = default)
    {
        return SendWithRefreshAsync(async () =>
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<TResponse>(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);

            string url = BuildUrl(path);
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Only attach token if present and not expired
            if (IsTokenValid())
            {
                string token = PlayerPrefs.GetString("AuthToken", "");
                if (!string.IsNullOrEmpty(token))
                    request.SetRequestHeader("Authorization", $"Bearer {token}");
            }

            var operation = request.SendWebRequest();

            if (cancellationToken != default)
            {
                cancellationToken.Register(() =>
                {
                    if (!operation.isDone)
                        request.Abort();
                    tcs.TrySetCanceled();
                });
            }

            operation.completed += _ =>
            {
                try
                {
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var resp = JsonUtility.FromJson<TResponse>(request.downloadHandler.text);
                        tcs.TrySetResult(resp);
                    }
                    else
                    {
                        var msg = ParseRequestError(request);
                        tcs.TrySetException(new Exception(msg));
                    }
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                finally
                {
                    request.Dispose();
                }
            };

            return await tcs.Task.ConfigureAwait(false);
        }, cancellationToken);
    }

    // Generic helper that runs the request factory, and on 401 attempts a refresh via RefreshTokenAsync once
    private static async System.Threading.Tasks.Task<TResult> SendWithRefreshAsync<TResult>(Func<System.Threading.Tasks.Task<TResult>> requestFactory, System.Threading.CancellationToken cancellationToken)
    {
        bool attemptedRefresh = false;

        while (true)
        {
            try
            {
                return await requestFactory().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // If we can detect a 401, attempt refresh. UnityWebRequest errors are wrapped as Exception with message from ParseRequestError.
                // Since ParseRequestError returns a message string, we need a more robust way: detect "401" text in the message or specialized exception.
                string msg = ex.Message ?? "";
                bool is401 = msg.Contains("401") || msg.ToLowerInvariant().Contains("unauthorized");

                if (is401 && !attemptedRefresh && RefreshTokenAsync != null)
                {
                    attemptedRefresh = true;
                    try
                    {
                        bool refreshed = await RefreshTokenAsync().ConfigureAwait(false);
                        if (refreshed)
                        {
                            // retry the request
                            continue;
                        }
                    }
                    catch (Exception refreshEx)
                    {
                        Debug.LogWarning($"ApiClient: Refresh attempt failed: {refreshEx}");
                    }
                }

                // If we reach here, refresh either not attempted, failed, or not supported. Clear token and notify.
                ClearAuthToken();
                try
                {
                    OnTokenExpired?.Invoke();
                }
                catch (Exception cbEx)
                {
                    Debug.LogError($"ApiClient: OnTokenExpired callback threw: {cbEx}");
                }

                throw new ApiUnauthorizedException("Unauthorized - token expired or invalid.");
            }
        }
    }
}
