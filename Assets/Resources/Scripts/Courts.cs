using Firebase.Extensions;
using Firebase.Firestore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class Courts : MonoBehaviour
{
    public static Courts Instance;

    [SerializeField] private GameObject m_CourtCardPrefab;
    [SerializeField] private ScrollRect m_CourtScrollRect;
    [SerializeField] private Transform m_CourtListTransform;

    [SerializeField] private GameObject m_CourtDetailPanel;
    [SerializeField] private Transform m_CourtDetail_Image;
    [SerializeField] private GameObject m_CourtDetail_CheckedIn;
    [SerializeField] private Image m_CourtDetail_Badge;
    [SerializeField] private TextMeshProUGUI m_CourtDetail_Name;
    [SerializeField] private TextMeshProUGUI m_CourtDetail_Address;
    [SerializeField] private Transform m_CourtDetail_PlayerList;
    [SerializeField] private GameObject m_CourtDetail_PlayerListItemPrefab;
    [SerializeField] private Transform m_CourtDetail_PlayerImage;
    [SerializeField] private TextMeshProUGUI m_CourtDetail_PlayerName;
    [SerializeField] private TextMeshProUGUI m_CourtDetail_PlayerPosition;
    [SerializeField] private TextMeshProUGUI m_CourtDetail_PlayerBadges;
    [SerializeField] private TextMeshProUGUI m_CourtDetail_PlayerRank;
    [SerializeField] private GameObject m_CourtDetail_PlayerMVP;
    [SerializeField] private TMP_Dropdown m_CourtDetail_PlayerStatus;
    [SerializeField] private Button m_CourtDetail_CheckInButton;

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
        if (!Input.location.isEnabledByUser)
            yield break;

        Input.location.Start(10, 0.1f);

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1)
        {
            m_LocationServiceFailedPopup.GetComponent<Animator>().Play("Modal Window In");
            m_LocationServiceFailedPopup_MessageText.text = "Location service initializing Timed out. Please try again later.";
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
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
                    yield break;
                }

                PlayerProfile myProfile = FirebaseManager.Instance.m_PlayerCardList.FirstOrDefault(item => item.UserID == userId);
                myProfile.CheckedInCourt = courtId;
                myProfile.Badges += 1;
                string courts = myProfile.VisitedCourts;
                List<int> visitedCourts = new List<int>();
                if (courts.Length > 0)
                {
                    visitedCourts = courts.Split(',').Select(int.Parse).ToList();
                }
                visitedCourts.Add(courtId);
                visitedCourts = visitedCourts.Distinct().ToList();
                myProfile.VisitedCourts = string.Join(",", visitedCourts.ToArray());
                myProfile.Status = m_CourtDetail_PlayerStatus.options[m_CourtDetail_PlayerStatus.value].text;
                _ = FirebaseManager.Instance.SetCheckInCourtAsync(myProfile);
            }
            else
            {
                m_LocationServiceFailedPopup.GetComponent<Animator>().Play("Modal Window In");
                m_LocationServiceFailedPopup_MessageText.text = "You can't check-in, you are not in the " + FirebaseManager.Instance.m_CourtList[courtId - 1].Name;
            }
        }

        Input.location.Stop();
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

        if (FirebaseManager.Instance == null || FirebaseManager.Instance.m_CourtList == null)
            return;

        foreach (Court court in FirebaseManager.Instance.m_CourtList)
        {
            AddCourt(court.ID, court.Name, court.Address, court.ImagePath);
        }

        UpdateCourtListLayout();
    }

    private void ClearCourtsList()
    {
        foreach (Transform cardTrans in m_CourtListTransform)
        {
            Destroy(cardTrans.gameObject);
        }

        m_CourtListTransform.DetachChildren();
    }

    private void AddCourt(int id, string name, string address, string imagePath = "")
    {
        GameObject card = Instantiate(m_CourtCardPrefab);
        card.transform.SetParent(m_CourtListTransform, false);
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
        Transform courtImage = m_CourtListTransform.GetChild(courtId - 1).GetChild(1).GetChild(0).GetChild(0);
        m_CourtDetail_Image.GetComponent<RawImage>().texture = courtImage.GetComponent<RawImage>().texture;
        m_CourtDetail_Image.GetComponent<AspectRatioFitter>().aspectMode = courtImage.GetComponent<AspectRatioFitter>().aspectMode;
        m_CourtDetail_Image.GetComponent<AspectRatioFitter>().aspectRatio = courtImage.GetComponent<AspectRatioFitter>().aspectRatio;
        m_CourtDetail_Name.text = m_CourtListTransform.GetChild(courtId - 1).GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text;
        m_CourtDetail_Address.text = m_CourtListTransform.GetChild(courtId - 1).GetChild(1).GetChild(2).GetComponent<TextMeshProUGUI>().text;
        m_CourtDetail_CheckedIn.SetActive(m_CourtListTransform.GetChild(courtId - 1).GetChild(1).GetChild(4).gameObject.activeSelf);
        Image badgeImage = m_CourtListTransform.GetChild(courtId - 1).GetChild(1).GetChild(5).GetComponent<Image>();
        m_CourtDetail_Badge.sprite = badgeImage.sprite;
        m_CourtDetail_Badge.color = badgeImage.color;

        for (int i = 0; i < m_CourtDetail_PlayerList.childCount; i ++)
        {
            Destroy(m_CourtDetail_PlayerList.GetChild(i).gameObject);
        }

        m_CourtDetail_PlayerList.DetachChildren();

        for (int i = 0; i < FirebaseManager.Instance.m_PlayerCardList.Count; i++)
        {
            if (FirebaseManager.Instance.m_PlayerCardList[i].VisitedCourts.Length == 0)
                continue;

            if (FirebaseManager.Instance.m_PlayerCardList[i].VisitedCourts.Contains(courtId.ToString()))
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
            m_CourtDetail_PlayerRank.text = myProfile.Rank == 0 ? "Rank NA" : "Rank " + myProfile.Rank;
            m_CourtDetail_PlayerMVP.SetActive(myProfile.IsMVP);

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

        m_CourtDetailPanel.SetActive(true);
    }

    private void CheckIn(int courtId)
    {
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
        m_CourtListTransform.GetChild(id - 1).GetChild(1).GetChild(0).GetChild(0).GetComponent<AspectRatioFitter>().aspectRatio = aspectRatio;
        m_CourtListTransform.GetChild(id - 1).GetChild(1).GetChild(0).GetChild(0).GetComponent<RawImage>().texture = texture;
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
        m_CourtScrollRect.GetComponent<UI_ScrollRectOcclusion>().Init();
    }

    public void CourtCheckedIn(int courtId)
    {
        //string userId = PlayerPrefs.GetString("UserID", string.Empty);
        //PlayerProfile myProfile = FirebaseManager.Instance.m_PlayerCardList.FirstOrDefault(item => item.UserID == userId);
        //if (myProfile != null)
        //{
        //    GameObject playerItem = Instantiate(m_CourtDetail_PlayerListItemPrefab);
        //    playerItem.transform.SetParent(m_CourtDetail_PlayerList, false);
        //    playerItem.name = myProfile.Name;
        //    if (myProfile.Image.Length > 0)
        //    {
        //        FileInfo fileInfo = new FileInfo(myProfile.Image);
        //        if (fileInfo.Exists)
        //        {
        //            MemoryStream dest = new MemoryStream();

        //            using (Stream source = fileInfo.OpenRead())
        //            {
        //                byte[] buffer = new byte[2048];
        //                int bytesRead;
        //                while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
        //                {
        //                    dest.Write(buffer, 0, bytesRead);
        //                }
        //            }

        //            byte[] imageBytes = dest.ToArray();

        //            Texture2D texture = new Texture2D(300, 300);
        //            texture.LoadImage(imageBytes);
        //            playerItem.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<RawImage>().texture = texture;
        //            playerItem.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        //            playerItem.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
        //        }
        //    }
        //    playerItem.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = myProfile.Name;
        //    playerItem.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = myProfile.TeamPosition;
        //    playerItem.transform.GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<TextMeshProUGUI>().text = myProfile.Badges == 0 ? "Not Available" : myProfile.Badges.ToString();
        //    playerItem.transform.GetChild(0).GetChild(1).GetChild(2).GetChild(1).GetComponent<TextMeshProUGUI>().text = myProfile.Rank == 0 ? "RANK NA" : "Rank " + myProfile.Rank;
        //    playerItem.transform.GetChild(0).GetChild(1).GetChild(2).GetChild(2).GetComponent<TextMeshProUGUI>().text = myProfile.Status;
        //    playerItem.transform.GetChild(0).GetChild(1).GetChild(2).GetChild(3).gameObject.SetActive(myProfile.IsMVP);
        //}

        //m_CourtDetail_CheckInButton.GetComponent<CanvasGroup>().alpha = 0.3f;
        //m_CourtDetail_CheckInButton.GetComponent<CanvasGroup>().interactable = false;
        //m_CourtDetail_CheckInButton.GetComponent<CanvasGroup>().blocksRaycasts = false;
    }
}
