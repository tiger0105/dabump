using System;

[Serializable]
public class Court
{
    public int ID;
    public string Name;
    public string Address;
    public string ImagePath;

    public Court(int id = 0, string name = "", string address = "", string imagePath = "")
    {
        ID = id;
        Name = name;
        Address = address;
        ImagePath = imagePath;
    }
}

[Serializable]
public class PlayerProfile
{
    public string UserID;
    public string Name;
    public string Image;
    public int Rank;
    public int Badges;
    public bool IsMVP;
    public int[] Courts;
    public string TeamPosition;
    public string CardTopColor;
    public string CardBottomColor;

    public PlayerProfile(string userId = "", string name = "", string image = "", int rank = 0, int badges = 0,
        bool isMVP = false, int[] courts = null, string teamPosition = "", string cardTopColor = "000000", string cardBottomColor = "000000")
    {
        UserID = userId;
        Name = name;
        Image = image;
        Rank = rank;
        Badges = badges;
        IsMVP = isMVP;
        Courts = courts;
        TeamPosition = teamPosition;
        CardTopColor = cardTopColor;
        CardBottomColor = cardBottomColor;
    }
}

public static class AppData
{
    public static string _REST_API_ENDPOINT = "https://us-central1-dabump-8c59c.cloudfunctions.net";
    public static string _REST_API_GET_MY_PROFILE = "/getMyProfile";
    public static string _REST_API_SUBMIT_HELP_FORM = "/submitHelpForm";
}