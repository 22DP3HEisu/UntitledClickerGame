using System;

/// <summary>
/// Response model for login API endpoint
/// </summary>
[Serializable]
public class LoginResponse
{
    public string message;
    public string token;
    public LoginUserData user;
}

/// <summary>
/// User data structure returned on login
/// </summary>
[Serializable]
public class LoginUserData
{
    public int id;
    public string username;
    public string email;
    public string role;
    public bool isBanned; // Added ban status field
    public string createdAt;
}

/// <summary>
/// Login request data structure
/// </summary>
[Serializable]
public class LoginData
{
    public string username;
    public string password;
}