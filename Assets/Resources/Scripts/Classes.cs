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
public class PlayerCard
{
    public string UserID;
    public string Name;
    public string ImagePath;
    public int Rank;
    public int Badges;
    public bool IsMVP;
    public string TeamPosition;
    public string CardTopColor;
    public string CardBottomColor;

    public PlayerCard(string userId = "", string name = "", string imagePath = "", int rank = 0, int badges = 0, 
        bool isMVP = false, string teamPosition = "", string cardTopColor = "000000", string cardBottomColor = "000000")
    {
        UserID = userId;
        Name = name;
        ImagePath = imagePath;
        Rank = rank;
        Badges = badges;
        IsMVP = isMVP;
        TeamPosition = teamPosition;
        CardTopColor = cardTopColor;
        CardBottomColor = cardBottomColor;
    }
}
