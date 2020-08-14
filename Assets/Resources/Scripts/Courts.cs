using Firebase.Extensions;
using Firebase.Firestore;
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

    [SerializeField] private List<Sprite> m_BadgeIcons;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        BuildCourtsList();
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
                playerItem.transform.GetChild(0).GetChild(1).GetChild(2).GetChild(3).gameObject.SetActive(profile.IsMVP);
            }
        }

        if (AppData._PlayerProfile.Image.Length > 0)
        {
            FileInfo playerImageFI = new FileInfo(AppData._PlayerProfile.Image);
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
        m_CourtDetail_PlayerName.text = AppData._PlayerProfile.Name;
        m_CourtDetail_PlayerPosition.text = AppData._PlayerProfile.TeamPosition;
        m_CourtDetail_PlayerBadges.text = AppData._PlayerProfile.Badges == 0 ? "Not Available" : AppData._PlayerProfile.Badges.ToString();
        m_CourtDetail_PlayerRank.text = AppData._PlayerProfile.Rank == 0 ? "Rank NA" : "Rank " + AppData._PlayerProfile.Rank;
        m_CourtDetail_PlayerMVP.SetActive(AppData._PlayerProfile.IsMVP);

        if (AppData._PlayerProfile.CheckedInCourt == courtId)
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

        m_CourtDetailPanel.SetActive(true);
    }

    public void CheckIn()
    {

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

    private async Task SetCheckInCourt(FirebaseFirestore firestore)
    {
        string userId = PlayerPrefs.GetString("UserID", string.Empty);
        if (userId == string.Empty)
        {
            return;
        }

        int[] courts = { 1, 2, 3, 4 };

        DocumentReference documentReference = firestore.Collection("Profiles").Document(userId);
        Dictionary<string, object> profile = new Dictionary<string, object>
        {
            ["Courts"] = courts,
        };

        await documentReference.SetAsync(profile).ContinueWithOnMainThread(task =>
        {
            Debug.Log("SetCheckInCourt Completed");
        });
    }
}
