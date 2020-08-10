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
