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
    [SerializeField] private ScrollRect m_CourtScrollRect;
    [SerializeField] private Transform m_Courts_ListTransform;

    [SerializeField] private ScrollRect m_RecentlyVisitedCourts_ScrollRect;
    [SerializeField] private Transform m_RecentlyVisitedCourts_ListTransform;

    [SerializeField] private GameObject m_CourtDetailPanel;
    [SerializeField] private Transform m_CourtDetail_Image;
    [SerializeField] private GameObject m_CourtDetail_CheckedIn;
    [SerializeField] private Image m_CourtDetail_Badge;
    [SerializeField] private TextMeshProUGUI m_CourtDetail_Name;
    [SerializeField] private TextMeshProUGUI m_CourtDetail_Address;

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

        m_Courts_ListTransform.GetComponent<HorizontalLayoutGroup>().enabled = true;
        m_Courts_ListTransform.GetComponent<ContentSizeFitter>().enabled = true;

        if (FirebaseManager.Instance == null || FirebaseManager.Instance.m_CourtList == null)
            return;

        foreach (Court court in FirebaseManager.Instance.m_CourtList)
        {
            AddCourt(court.ID, court.Name, court.Address, court.ImagePath);
        }

        UI_ScrollRectOcclusion occlusion = m_CourtScrollRect.gameObject.AddComponent<UI_ScrollRectOcclusion>();
        occlusion.InitByUser = true;

        UpdateCourtListLayout();
    }

    public void BuildRecentlyVisitedCourtsList()
    {
        ClearRecentlyVisitedCourtsList();

        m_RecentlyVisitedCourts_ListTransform.GetComponent<HorizontalLayoutGroup>().enabled = true;
        m_RecentlyVisitedCourts_ListTransform.GetComponent<ContentSizeFitter>().enabled = true;

        string userId = PlayerPrefs.GetString("UserID", string.Empty);
        PlayerProfile myProfile = FirebaseManager.Instance.m_PlayerCardList.FirstOrDefault(item => item.UserID == userId);
        if (myProfile != null)
        {
            string courts = myProfile.VisitedCourts;
            List<int> visitedCourts = new List<int>();
            if (courts.Length > 0)
            {
                visitedCourts = courts.Split(',').Select(int.Parse).ToList();
            }
            
            for (int i = 0; i < visitedCourts.Count; i ++)
            {
                int id = visitedCourts[i] - 1;
                GameObject visitedCard = Instantiate(m_Courts_ListTransform.GetChild(id).gameObject);
                visitedCard.transform.SetParent(m_RecentlyVisitedCourts_ListTransform, false);
                visitedCard.name = name;
                Button cardButton = visitedCard.transform.GetChild(1).GetComponent<Button>();
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(delegate { ShowCourtDetailPanel(id + 1); });
            }
        }

        UI_ScrollRectOcclusion occlusion = m_RecentlyVisitedCourts_ScrollRect.gameObject.AddComponent<UI_ScrollRectOcclusion>();
        occlusion.InitByUser = true;

        UpdateRecentlyVisitedCourtsListLayout();
    }

    private void ClearCourtsList()
    {
        if (m_CourtScrollRect.GetComponent<UI_ScrollRectOcclusion>())
        {
            Destroy(m_CourtScrollRect.GetComponent<UI_ScrollRectOcclusion>());
        }

        foreach (Transform cardTrans in m_Courts_ListTransform)
        {
            Destroy(cardTrans.gameObject);
        }

        m_Courts_ListTransform.DetachChildren();
    }

    private void ClearRecentlyVisitedCourtsList()
    {
        if (m_RecentlyVisitedCourts_ScrollRect.GetComponent<UI_ScrollRectOcclusion>())
        {
            Destroy(m_RecentlyVisitedCourts_ScrollRect.GetComponent<UI_ScrollRectOcclusion>());
        }

        foreach (Transform cardTrans in m_RecentlyVisitedCourts_ListTransform)
        {
            Destroy(cardTrans.gameObject);
        }

        m_RecentlyVisitedCourts_ListTransform.DetachChildren();
    }

    private void UpdateRecentlyVisitedCourtsListLayout()
    {
        StartCoroutine(ApplyRecentlyVisitedCourtsListScrollPosition());
    }

    private void AddCourt(int id, string name, string address, string imagePath = "")
    {
        GameObject card = Instantiate(m_CourtCardPrefab);
        card.transform.SetParent(m_Courts_ListTransform, false);
        card.name = name;
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
        Transform courtImage = m_Courts_ListTransform.GetChild(courtId - 1).GetChild(1).GetChild(0).GetChild(0);
        m_CourtDetail_Image.GetComponent<RawImage>().texture = courtImage.GetComponent<RawImage>().texture;
        m_CourtDetail_Image.GetComponent<AspectRatioFitter>().aspectMode = courtImage.GetComponent<AspectRatioFitter>().aspectMode;
        m_CourtDetail_Image.GetComponent<AspectRatioFitter>().aspectRatio = courtImage.GetComponent<AspectRatioFitter>().aspectRatio;
        m_CourtDetail_Name.text = m_Courts_ListTransform.GetChild(courtId - 1).GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text;
        m_CourtDetail_Address.text = m_Courts_ListTransform.GetChild(courtId - 1).GetChild(1).GetChild(2).GetComponent<TextMeshProUGUI>().text;
        m_CourtDetail_CheckedIn.SetActive(m_Courts_ListTransform.GetChild(courtId - 1).GetChild(1).GetChild(4).gameObject.activeSelf);
        Image badgeImage = m_Courts_ListTransform.GetChild(courtId - 1).GetChild(1).GetChild(5).GetComponent<Image>();
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
                    if (fileInfo.Exists)
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

        string userId = PlayerPrefs.GetString("UserID", string.Empty);
        PlayerProfile myProfile = FirebaseManager.Instance.m_PlayerCardList.FirstOrDefault(item => item.UserID == userId);
        if (myProfile != null)
        {
            if (myProfile.Image.Length > 0)
            {
                FileInfo playerImageFI = new FileInfo(myProfile.Image);
                if (playerImageFI.Exists)
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
                CheckIn(courtId);
            });

            if (myProfile.CheckedInCourt == courtId)
            {
                m_CourtDetail_CheckInButton.GetComponent<CanvasGroup>().alpha = 0.3f;
                m_CourtDetail_CheckInButton.GetComponent<CanvasGroup>().interactable = false;
                m_CourtDetail_CheckInButton.GetComponent<CanvasGroup>().blocksRaycasts = false;
            }
            else
            {
                m_CourtDetail_CheckInButton.GetComponent<CanvasGroup>().alpha = 1;
                m_CourtDetail_CheckInButton.GetComponent<CanvasGroup>().interactable = true;
                m_CourtDetail_CheckInButton.GetComponent<CanvasGroup>().blocksRaycasts = true;
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

    public void SetCourtImage(int id, string imagePath)
    {
        FileInfo fileInfo = new FileInfo(imagePath);

        if (!fileInfo.Exists) return;

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
        m_Courts_ListTransform.GetChild(id - 1).GetChild(1).GetChild(0).GetChild(0).GetComponent<AspectRatioFitter>().aspectRatio = aspectRatio;
        m_Courts_ListTransform.GetChild(id - 1).GetChild(1).GetChild(0).GetChild(0).GetComponent<RawImage>().texture = texture;
    }

    private void UpdateCourtListLayout()
    {
        StartCoroutine(ApplyScrollPosition());
    }

    private IEnumerator ApplyScrollPosition()
    {
        yield return new WaitForEndOfFrame();
        m_CourtScrollRect.horizontalNormalizedPosition = 0;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_CourtScrollRect.transform);
        m_CourtScrollRect.GetComponent<UI_ScrollRectOcclusion>().enabled = true;
        m_CourtScrollRect.GetComponent<UI_ScrollRectOcclusion>().Init();
    }

    private IEnumerator ApplyRecentlyVisitedCourtsListScrollPosition()
    {
        if (m_RecentlyVisitedCourts_ListTransform.childCount > 0)
        {
            Home.Instance.m_CourtScrollRect.gameObject.SetActive(true);
            Home.Instance.m_GetStartedPanel.SetActive(false);
            yield return new WaitForEndOfFrame();
            m_RecentlyVisitedCourts_ScrollRect.horizontalNormalizedPosition = 0;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_RecentlyVisitedCourts_ScrollRect.transform);
            m_RecentlyVisitedCourts_ScrollRect.GetComponent<UI_ScrollRectOcclusion>().enabled = true;
            m_RecentlyVisitedCourts_ScrollRect.GetComponent<UI_ScrollRectOcclusion>().Init();
        }
        else
        {
            Home.Instance.m_CourtScrollRect.gameObject.SetActive(false);
            Home.Instance.m_GetStartedPanel.SetActive(true);
        }
    }

    public IEnumerator CourtCheckedIn(PlayerProfile myProfile)
    {
        yield return new WaitForEndOfFrame();
        BuildCourtsList();
        yield return new WaitForEndOfFrame();
        Profile.Instance.SetCourtCheckedInAndBadgeStatus(myProfile);
        yield return new WaitForEndOfFrame();
        BuildRecentlyVisitedCourtsList();
        yield return new WaitForEndOfFrame();
        ShowCourtDetailPanel(myProfile.CheckedInCourt);
        yield return new WaitForEndOfFrame();

        m_CourtDetail_LoadingBar.SetActive(false);
    }
}
