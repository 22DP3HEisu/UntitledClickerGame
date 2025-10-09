using System;

[Serializable]
public class UserProfileResponse
{
    public string message;
    public UserProfile user;

    [Serializable]
    public class UserProfile
    {
        public int id;
        public string username;
        public string email;
        public string createdAt;
        public GameData gameData;
    }

    [Serializable]
    public class GameData
    {
        public int carrots;
        public int horseShoes;
        public int goldenCarrots;
        public Upgrades upgrades;
    }

    [Serializable]
    public class Upgrades
    {
        public bool upgrade1;
        public bool upgrade2;
        public bool upgrade3;
    }
}
