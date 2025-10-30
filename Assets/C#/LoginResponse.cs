using System;

// Response model for login API endpoint
[Serializable]
public class LoginResponse
{
    public string message;
    public string token;
    public LoginUserData user;
}

// User data structure returned on login
[Serializable]
public class LoginUserData
{
    public int id;
    public string username;
    public string email;
    public string role;
    public bool isBanned;
    public string createdAt;
}

// Login request data structure
[Serializable]
public class LoginData
{
    public string username;
    public string password;
}