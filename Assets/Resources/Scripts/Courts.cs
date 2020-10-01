using Michsky.UI.Frost;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using static TMPro.TMP_Dropdown;

public class Courts : MonoBehaviour
{
    public static Courts Instance;

    [SerializeField] private GameObject m_CourtCardPrefab;
    
    [SerializeField] private ScrollRect m_Indoor_CourtScrollRect;
    [SerializeField] private Transform m_Indoor_Courts_ListTransform;

    [SerializeField] private ScrollRect m_Outdoor_CourtScrollRect;
    [SerializeField] private Transform m_Outdoor_Courts_ListTransform;

    [SerializeField] private GameObject m_CourtDetailPanel;
    [SerializeField] private Transform m_CourtDetail_Image;
    [SerializeField] private GameObject m_CourtDetail_CheckedIn;
    [SerializeField] private Image m_CourtDetail_Badge;
    [SerializeField] private TextMeshProUGUI m_CourtDetail_Name;
    [SerializeField] private TextMeshProUGUI m_CourtDetail_Address;
    [SerializeField] private Button m_CourtDetail_LocateWithGoogleMapButton;

    [SerializeField] private Transform m_CourtDetail_PlayerList;
    [SerializeField] private GameObject m_CourtDetail_PlayerListItemPrefab;
    [SerializeField] private GameObject m_CourtDetail_NoPlayersMessage;

    [SerializeField] private Transform m_CourtDetail_PlayerImage;
    [SerializeField] private TextMeshProUGUI m_CourtDetail_PlayerName;
    [SerializeField] private TextMeshProUGUI m_CourtDetail_PlayerPosition;
    [SerializeField] private TextMeshProUGUI m_CourtDetail_PlayerBadges;
    [SerializeField] private TextMeshProUGUI m_CourtDetail_PlayerRank;
    [SerializeField] private GameObject m_CourtDetail_PlayerMVP;
    [SerializeField] private TMP_Dropdown m_CourtDetail_PlayerStatus;
    [SerializeField] private Button m_CourtDetail_CheckInButton;
    [SerializeField] private Button m_CourtDetail_CheckOutButton;

    [SerializeField] public GameObject m_CourtDetail_LoadingBar;

    [SerializeField] private GameObject m_LocationServiceFailedPopup;
    [SerializeField] private TextMeshProUGUI m_LocationServiceFailedPopup_MessageText;

    [SerializeField] private List<Sprite> m_BadgeIcons;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        BuildCourtsList();
    }

    private IEnumerator StartLocationServiceAndCheckIn(int courtId)
    {
#if UNITY_EDITOR
        //#if !FOR_GPS_TEST
        string userId = PlayerPrefs.GetString("UserID", "X6iO1w2GebNDkf4nQsWLYqovIQh2");
        if (userId == string.Empty)
        {
            m_CourtDetail_LoadingBar.SetActive(false);
            yield break;
        }

        PlayerProfile myProfile = FirebaseManager.Instance.m_PlayerCardList.FirstOrDefault(item => item.UserID == userId);
        if (myProfile != null)
        {
            myProfile.CheckedInCourt = courtId;
            string courts = myProfile.VisitedCourts;
            List<int> visitedCourts = new List<int>();
            if (courts.Length > 0)
            {
                visitedCourts = courts.Split(',').Select(int.Parse).ToList();
            }
            visitedCourts.Add(courtId);
            visitedCourts = visitedCourts.Distinct().ToList();
            myProfile.VisitedCourts = string.Join(",", visitedCourts.ToArray());
            myProfile.Badges = visitedCourts.Count;
            myProfile.Status = m_CourtDetail_PlayerStatus.options[m_CourtDetail_PlayerStatus.value].text;
            _ = FirebaseManager.Instance.SetCheckInCourtAsync(myProfile);
        }
        else
        {
            m_CourtDetail_LoadingBar.SetActive(false);
            m_LocationServiceFailedPopup.GetComponent<Animator>().Play("Modal Window In");
            m_LocationServiceFailedPopup_MessageText.text = "Unexpected error occured. Please restart the application.";
        }
#else
        if (!Input.location.isEnabledByUser)
        {
            m_CourtDetail_LoadingBar.SetActive(false);
            m_LocationServiceFailedPopup.GetComponent<Animator>().Play("Modal Window In");
            m_LocationServiceFailedPopup_MessageText.text = "Location service is not enabled. Please enable it and restart the app.";
            yield break;
        }

        Input.location.Start(10, 0.1f);

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1)
        {
            m_CourtDetail_LoadingBar.SetActive(false);
            m_LocationServiceFailedPopup.GetComponent<Animator>().Play("Modal Window In");
            m_LocationServiceFailedPopup_MessageText.text = "Location service initializing Timed out. Please try again later.";
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            m_CourtDetail_LoadingBar.SetActive(false);
            m_LocationServiceFailedPopup.GetComponent<Animator>().Play("Modal Window In");
            m_LocationServiceFailedPopup_MessageText.text = "Unable to determine your location. Please try again later.";
            yield break;
        }
        else
        {
            double latitude = Input.location.lastData.latitude;
            double longitude = Input.location.lastData.longitude;
            double distance = DistanceInKmBetweenEarthCoordinates(latitude, longitude, FirebaseManager.Instance.m_CourtList[courtId - 1].Location.Latitude, FirebaseManager.Instance.m_CourtList[courtId - 1].Location.Longitude);
            if (distance < 0.1f)
            {
                string userId = PlayerPrefs.GetString("UserID", string.Empty);
                if (userId == string.Empty)
                {
                    m_CourtDetail_LoadingBar.SetActive(false);
                    yield break;
                }

                PlayerProfile myProfile = FirebaseManager.Instance.m_PlayerCardList.FirstOrDefault(item => item.UserID == userId);
                if (myProfile != null)
                {
                    myProfile.CheckedInCourt = courtId;
                    string courts = myProfile.VisitedCourts;
                    List<int> visitedCourts = new List<int>();
                    if (courts.Length > 0)
                    {
                        visitedCourts = courts.Split(',').Select(int.Parse).ToList();
                    }
                    visitedCourts.Add(courtId);
                    visitedCourts = visitedCourts.Distinct().ToList();
                    myProfile.VisitedCourts = string.Join(",", visitedCourts.ToArray());
                    myProfile.Badges = visitedCourts.Count;
                    myProfile.Status = m_CourtDetail_PlayerStatus.options[m_CourtDetail_PlayerStatus.value].text;
                    _ = FirebaseManager.Instance.SetCheckInCourtAsync(myProfile);
                }
                else
                {
                    m_CourtDetail_LoadingBar.SetActive(false);
                    m_LocationServiceFailedPopup.GetComponent<Animator>().Play("Modal Window In");
                    m_LocationServiceFailedPopup_MessageText.text = "Unexpected error occured. Please restart the application.";
                }
            }
            else
            {
                m_CourtDetail_LoadingBar.SetActive(false);
                m_LocationServiceFailedPopup.GetComponent<Animator>().Play("Modal Window In");
                m_LocationServiceFailedPopup_MessageText.text = "You can't check-in, you are not in the " + FirebaseManager.Instance.m_CourtList[courtId - 1].Name;
            }
        }

        Input.location.Stop();
#endif
    }

    private double DegreesToRadians(double degrees)
    {
        return degrees * Mathf.PI / 180;
    }

    double DistanceInKmBetweenEarthCoordinates(double lat1, double long1, double lat2, double long2)
    {
        double earthRadiusKm = 6378.14f;

        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(long2 - long1);

        lat1 = DegreesToRadians(lat1);
        lat2 = DegreesToRadians(lat2);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private void BuildCourtsList()
    {
        ClearCourtsList();

        m_Indoor_Courts_ListTransform.GetComponent<HorizontalLayoutGroup>().enabled = true;
        m_Indoor_Courts_ListTransform.GetComponent<ContentSizeFitter>().enabled = true;

        m_Outdoor_Courts_ListTransform.GetComponent<HorizontalLayoutGroup>().enabled = true;
        m_Outdoor_Courts_ListTransform.GetComponent<ContentSizeFitter>().enabled = true;

        if (FirebaseManager.Instance == null || FirebaseManager.Instance.m_CourtList == null)
            return;

        foreach (Court court in FirebaseManager.Instance.m_CourtList)
        {
            AddCourt(court.ID, court.Name, court.Address, court.ImagePath, court.Type);
        }

        UI_ScrollRectOcclusion occlusion = m_Indoor_CourtScrollRect.gameObject.AddComponent<UI_ScrollRectOcclusion>();
        occlusion.InitByUser = true;

        UI_ScrollRectOcclusion occlusion2 = m_Outdoor_CourtScrollRect.gameObject.AddComponent<UI_ScrollRectOcclusion>();
        occlusion2.InitByUser = true;

        UpdateCourtListLayout();
    }

    private void ClearCourtsList()
    {
        if (m_Indoor_CourtScrollRect.GetComponent<UI_ScrollRectOcclusion>())
        {
            Destroy(m_Indoor_CourtScrollRect.GetComponent<UI_ScrollRectOcclusion>());
        }

        foreach (Transform cardTrans in m_Indoor_Courts_ListTransform)
        {
            Destroy(cardTrans.gameObject);
        }

        m_Indoor_Courts_ListTransform.DetachChildren();

        if (m_Outdoor_CourtScrollRect.GetComponent<UI_ScrollRectOcclusion>())
        {
            Destroy(m_Outdoor_CourtScrollRect.GetComponent<UI_ScrollRectOcclusion>());
        }

        foreach (Transform cardTrans in m_Outdoor_Courts_ListTransform)
        {
            Destroy(cardTrans.gameObject);
        }

        m_Outdoor_Courts_ListTransform.DetachChildren();
    }

    private void AddCourt(int id, string name, string address, string imagePath = "", string type = "INDOOR")
    {
        GameObject card = Instantiate(m_CourtCardPrefab);
        if (type == "INDOOR")
        {
            card.transform.SetParent(m_Indoor_Courts_ListTransform, false);
        }
        else
        {
            card.transform.SetParent(m_Outdoor_Courts_ListTransform, false);
        }
        card.name = id.ToString();
        Button cardButton = card.transform.GetChild(1).GetComponent<Button>();
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(delegate { ShowCourtDetailPanel(id); });
        card.transform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = name;
        card.transform.GetChild(1).GetChild(2).GetComponent<TextMeshProUGUI>().text = address;
        string playersList = string.Empty;
        List<string> checkedInPlayers = new List<string>();
        for (int i = 0; i < FirebaseManager.Instance.m_PlayerCardList.Count; i ++)
        {
            if (FirebaseManager.Instance.m_PlayerCardList[i].VisitedCourts.Length == 0)
                continue;

            if (FirebaseManager.Instance.m_PlayerCardList[i].VisitedCourts.Contains(id.ToString()))
                checkedInPlayers.Add(FirebaseManager.Instance.m_PlayerCardList[i].Name);
        }
        if (checkedInPlayers.Count == 0)
            playersList = "No checked-in players";
        else
            playersList = checkedInPlayers.Aggregate((a, x) => a + ", " + x).ToString();
        card.transform.GetChild(1).GetChild(3).GetComponent<TextMeshProUGUI>().text = playersList;

        card.transform.GetChild(1).GetChild(5).GetComponent<Image>().sprite = m_BadgeIcons[id - 1];

        SetCourtImage(id, imagePath);
    }

    private void ShowCourtDetailPanel(int courtId)
    {
        Transform trans = null;
        foreach (Transform t in m_Indoor_Courts_ListTransform)
        {
            if (t.name == courtId.ToString())
            {
                trans = t;
                break;
            }
        }
        foreach (Transform t in m_Outdoor_Courts_ListTransform)
        {
            if (t.name == courtId.ToString())
            {
                trans = t;
                break;
            }
        }
        if (trans == null)
            return;

        Transform courtImage = trans.GetChild(1).GetChild(0).GetChild(0);
        m_CourtDetail_Image.GetComponent<RawImage>().texture = courtImage.GetComponent<RawImage>().texture;
        m_CourtDetail_Image.GetComponent<AspectRatioFitter>().aspectMode = courtImage.GetComponent<AspectRatioFitter>().aspectMode;
        m_CourtDetail_Image.GetComponent<AspectRatioFitter>().aspectRatio = courtImage.GetComponent<AspectRatioFitter>().aspectRatio;
        m_CourtDetail_Name.text = trans.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text;
        m_CourtDetail_Address.text = trans.GetChild(1).GetChild(2).GetComponent<TextMeshProUGUI>().text;
        m_CourtDetail_LocateWithGoogleMapButton.onClick.RemoveAllListeners();
        string url = FirebaseManager.Instance.m_CourtList[courtId - 1].Url;
        if (url.Length > 0)
        {
            m_CourtDetail_LocateWithGoogleMapButton.onClick.AddListener(delegate
            {
                Application.OpenURL(url);
            });
        }
        m_CourtDetail_CheckedIn.SetActive(trans.GetChild(1).GetChild(4).gameObject.activeSelf);
        Image badgeImage = trans.GetChild(1).GetChild(5).GetComponent<Image>();
        m_CourtDetail_Badge.sprite = badgeImage.sprite;
        m_CourtDetail_Badge.color = badgeImage.color;

        for (int i = 0; i < m_CourtDetail_PlayerList.childCount; i ++)
        {
            Destroy(m_CourtDetail_PlayerList.GetChild(i).gameObject);
        }

        m_CourtDetail_PlayerList.DetachChildren();

        for (int i = 0; i < FirebaseManager.Instance.m_PlayerCardList.Count; i++)
        {
            if (FirebaseManager.Instance.m_PlayerCardList[i].CheckedInCourt == 0)
                continue;

            if (FirebaseManager.Instance.m_PlayerCardList[i].CheckedInCourt == courtId)
            {
                PlayerProfile profile = FirebaseManager.Instance.m_PlayerCardList[i];
                GameObject playerItem = Instantiate(m_CourtDetail_PlayerListItemPrefab);
                playerItem.transform.SetParent(m_CourtDetail_PlayerList, false);
                playerItem.name = profile.Name;
                if (profile.Image.Length > 0)
                {
                    FileInfo fileInfo = new FileInfo(profile.Image);

                    if (fileInfo != null && fileInfo.Exists)
                    {
                        MemoryStream dest = new MemoryStream();

                        using (Stream source = fileInfo.OpenRead())
                        {
                            byte[] buffer = new byte[2048];
                            int bytesRead;
                            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                dest.Write(buffer, 0, bytesRead);
                            }
                        }

                        byte[] imageBytes = dest.ToArray();

                        Texture2D texture = new Texture2D(300, 300);
                        texture.LoadImage(imageBytes);
                        playerItem.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<RawImage>().texture = texture;
                        playerItem.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                        playerItem.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
                    }
                }
                playerItem.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = profile.Name;
                playerItem.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = profile.TeamPosition;
                playerItem.transform.GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<TextMeshProUGUI>().text = profile.Badges == 0 ? "Not Available" : profile.Badges.ToString();
                playerItem.transform.GetChild(0).GetChild(1).GetChild(2).GetChild(1).GetComponent<TextMeshProUGUI>().text = profile.Rank == 0 ? "RANK NA" : "Rank " + profile.Rank;
                playerItem.transform.GetChild(0).GetChild(1).GetChild(2).GetChild(2).GetComponent<TextMeshProUGUI>().text = profile.Status;
                playerItem.transform.GetChild(0).GetChild(1).GetChild(2).GetChild(3).gameObject.SetActive(profile.IsMVP);
            }
        }

#if UNITY_EDITOR
        string userId = PlayerPrefs.GetString("UserID", "X6iO1w2GebNDkf4nQsWLYqovIQh2");
#else
        string userId = PlayerPrefs.GetString("UserID", string.Empty);
#endif
        PlayerProfile myProfile = FirebaseManager.Instance.m_PlayerCardList.FirstOrDefault(item => item.UserID == userId);
        if (myProfile != null)
        {
            if (myProfile.Image.Length > 0)
            {
                FileInfo playerImageFI = new FileInfo(myProfile.Image);
                if (playerImageFI != null && playerImageFI.Exists)
                {
                    MemoryStream dest = new MemoryStream();

                    using (Stream source = playerImageFI.OpenRead())
                    {
                        byte[] buffer = new byte[2048];
                        int bytesRead;
                        while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            dest.Write(buffer, 0, bytesRead);
                        }
                    }

                    byte[] imageBytes = dest.ToArray();

                    Texture2D texture = new Texture2D(500, 500);
                    texture.LoadImage(imageBytes);
                    m_CourtDetail_PlayerImage.GetComponent<RawImage>().texture = texture;
                    m_CourtDetail_PlayerImage.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    m_CourtDetail_PlayerImage.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
                }
            }
            m_CourtDetail_PlayerName.text = myProfile.Name;
            m_CourtDetail_PlayerPosition.text = myProfile.TeamPosition;
            m_CourtDetail_PlayerBadges.text = myProfile.Badges == 0 ? "Not Available" : myProfile.Badges.ToString();
            Profile.Instance.m_RightPanel_Badge.text = m_CourtDetail_PlayerBadges.text;
            m_CourtDetail_PlayerRank.text = myProfile.Rank == 0 ? "Rank NA" : "Rank " + myProfile.Rank;
            m_CourtDetail_PlayerMVP.SetActive(myProfile.IsMVP);
            int index = m_CourtDetail_PlayerStatus.options.FindIndex(item => item.text == myProfile.Status);
            if (index >= 0)
            {
                m_CourtDetail_PlayerStatus.value = index;
            }

            m_CourtDetail_CheckInButton.GetComponent<Button>().onClick.RemoveAllListeners();
            m_CourtDetail_CheckInButton.GetComponent<Button>().onClick.AddListener(delegate
            {
                Debug.Log("CheckIn");
                CheckIn(courtId);
            });

            m_CourtDetail_CheckOutButton.GetComponent<Button>().onClick.RemoveAllListeners();
            m_CourtDetail_CheckOutButton.GetComponent<Button>().onClick.AddListener(delegate
            {
                Debug.Log("CheckOut");
                CheckOut();
            });

            if (myProfile.CheckedInCourt == courtId)
            {
                m_CourtDetail_CheckInButton.gameObject.SetActive(false);
                m_CourtDetail_CheckOutButton.gameObject.SetActive(true);
            }
            else
            {
                m_CourtDetail_CheckInButton.gameObject.SetActive(true);
                m_CourtDetail_CheckOutButton.gameObject.SetActive(false);
            }
        }

        if (m_CourtDetail_PlayerList.childCount > 0)
        {
            m_CourtDetail_NoPlayersMessage.SetActive(false);
        }
        else
        {
            m_CourtDetail_NoPlayersMessage.SetActive(true);
        }

        m_CourtDetailPanel.SetActive(true);
    }

    private void CheckIn(int courtId)
    {
        m_CourtDetail_LoadingBar.SetActive(true);

        StartCoroutine(StartLocationServiceAndCheckIn(courtId));
    }

    private void CheckOut()
    {
        m_CourtDetail_LoadingBar.SetActive(true);

        StartCoroutine(StartLocationServiceAndCheckIn(0));
    }

    public void SetCourtImage(int courtId, string imagePath)
    {
        Transform trans = null;
        foreach (Transform t in m_Indoor_Courts_ListTransform)
        {
            if (t.name == courtId.ToString())
            {
                trans = t;
                break;
            }
        }
        foreach (Transform t in m_Outdoor_Courts_ListTransform)
        {
            if (t.name == courtId.ToString())
            {
                trans = t;
                break;
            }
        }
        if (trans == null)
            return;

        FileInfo fileInfo = new FileInfo(imagePath);

        if (fileInfo == null || !fileInfo.Exists) return;

        MemoryStream dest = new MemoryStream();

        using (Stream source = fileInfo.OpenRead())
        {
            byte[] buffer = new byte[2048];
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                dest.Write(buffer, 0, bytesRead);
            }
        }

        byte[] imageBytes = dest.ToArray();

        Texture2D texture = new Texture2D(500, 580);
        texture.LoadImage(imageBytes);
        float aspectRatio = (float)texture.width / texture.height;
        trans.GetChild(1).GetChild(0).GetChild(0).GetComponent<AspectRatioFitter>().aspectRatio = aspectRatio;
        trans.GetChild(1).GetChild(0).GetChild(0).GetComponent<RawImage>().texture = texture;
    }

    private void UpdateCourtListLayout()
    {
        StartCoroutine(ApplyScrollPosition());
    }

    private IEnumerator ApplyScrollPosition()
    {
        yield return new WaitForEndOfFrame();
        m_Indoor_CourtScrollRect.horizontalNormalizedPosition = 0;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_Indoor_CourtScrollRect.transform);
        m_Indoor_CourtScrollRect.GetComponent<UI_ScrollRectOcclusion>().enabled = true;
        m_Indoor_CourtScrollRect.GetComponent<UI_ScrollRectOcclusion>().Init();

        yield return new WaitForEndOfFrame();
        m_Outdoor_CourtScrollRect.horizontalNormalizedPosition = 0;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_Outdoor_CourtScrollRect.transform);
        m_Outdoor_CourtScrollRect.GetComponent<UI_ScrollRectOcclusion>().enabled = true;
        m_Outdoor_CourtScrollRect.GetComponent<UI_ScrollRectOcclusion>().Init();

        m_Indoor_Root.SetActive(true);
        m_Outdoor_Root.SetActive(false);
    }

    public IEnumerator CourtCheckedIn(PlayerProfile myProfile)
    {
        yield return new WaitForEndOfFrame();
        BuildCourtsList();
        yield return new WaitForEndOfFrame();
        Profile.Instance.SetCourtCheckedInAndBadgeStatus(myProfile);
        yield return new WaitForEndOfFrame();
        ShowCourtDetailPanel(myProfile.CheckedInCourt);
        yield return new WaitForEndOfFrame();

        m_CourtDetail_LoadingBar.SetActive(false);
    }

    [SerializeField] private ToggleGroup m_Tab;
    [SerializeField] private GameObject m_Indoor_Root;
    [SerializeField] private GameObject m_Outdoor_Root;

    public void TabButtonClicked(bool isOn)
    {
        if (isOn == true)
        {
            Toggle activeToggle = m_Tab.ActiveToggles().FirstOrDefault();
            if (activeToggle != null)
            {
                int tabIndex = activeToggle.transform.GetSiblingIndex();
                if (tabIndex == 0)
                {
                    m_Indoor_Root.SetActive(true);
                    m_Outdoor_Root.SetActive(false);
                }
                else if (tabIndex == 1)
                {
                    m_Indoor_Root.SetActive(false);
                    m_Outdoor_Root.SetActive(true);
                }
            }
        }
    }
}
