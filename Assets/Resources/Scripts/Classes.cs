using System;
using System.Collections.Generic;

[Serializable]
public class Court
{
    public int ID;
    public string Name;
    public string Address;
    public string ImagePath;
    public bool IsCheckedIn;
    public bool HasBadge;
    public List<string> CheckedInPlayers;

    public Court(int id = 0, string name = "", string address = "", string imagePath = "", 
        bool isCheckedIn = false, bool hasBadge = false, List<string> checkedInPlayers = null)
    {
        ID = id;
        Name = name;
        Address = address;
        ImagePath = imagePath;
        IsCheckedIn = isCheckedIn;
        HasBadge = hasBadge;
        if (checkedInPlayers == null)
            CheckedInPlayers = new List<string>();
        else
            CheckedInPlayers = checkedInPlayers;
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
    public int ActiveCourt;
    public string TeamPosition;
    public string CardTopColor;
    public string CardBottomColor;

    public PlayerProfile(string userId = "", string name = "", string image = "", int rank = 0, int badges = 0,
        bool isMVP = false, int activeCourt = 0, string teamPosition = "", string cardTopColor = "000000", string cardBottomColor = "000000")
    {
        UserID = userId;
        Name = name;
        Image = image;
        Rank = rank;
        Badges = badges;
        IsMVP = isMVP;
        ActiveCourt = activeCourt;
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