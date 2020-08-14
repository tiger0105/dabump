using Firebase.Firestore;
using System;
using System.Collections.Generic;

[Serializable]
public class Court
{
    public int ID;
    public string Name;
    public string Address;
    public string ImagePath;
    public List<string> CheckedInPlayers;
    public string Url;
    public GeoPoint Location;

    public Court(GeoPoint location, int id = 0, string name = "", string address = "", string imagePath = "", List<string> checkedInPlayers = null, string url = "")
    {
        Location = location;
        ID = id;
        Name = name;
        Address = address;
        ImagePath = imagePath;
        if (checkedInPlayers == null)
            CheckedInPlayers = new List<string>();
        else
            CheckedInPlayers = checkedInPlayers;
        Url = url;
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
    public int CheckedInCourt;
    public string VisitedCourts;
    public string Status;
    public string TeamPosition;
    public string CardTopColor;
    public string CardBottomColor;

    public PlayerProfile(string userId = "", string name = "", string image = "", int rank = 0, int badges = 0,
        bool isMVP = false, int checkedInCourt = 0, string visitedCourts = "", string status = "ACTIVE", string teamPosition = "", string cardTopColor = "000000", string cardBottomColor = "000000")
    {
        UserID = userId;
        Name = name;
        Image = image;
        Rank = rank;
        Badges = badges;
        IsMVP = isMVP;
        CheckedInCourt = checkedInCourt;
        VisitedCourts = visitedCourts;
        Status = status;
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

    public static PlayerProfile _PlayerProfile = new PlayerProfile();
}