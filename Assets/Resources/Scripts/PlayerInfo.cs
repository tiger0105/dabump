using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfo : MonoBehaviour
{
    public static PlayerInfo Instance;

    [Header("PlayerInfo")]
    [SerializeField] private GameObject m_PlayerCardPrefab;
    [SerializeField] private ScrollRect m_PlayerInfoScrollRect;
    [SerializeField] private Transform m_PlayerInfoListTransform;

    [Header("Right Panel")]
    [SerializeField] private GameObject m_RightPanel_TopPlayer_Prefab;
    [SerializeField] private Transform m_RightPanel_TopPlayer_List;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GridLayoutGroup gridLayoutGroup = m_PlayerInfoListTransform.GetComponent<GridLayoutGroup>();
        float space = m_PlayerInfoListTransform.GetComponent<RectTransform>().rect.width - 320 * 4;
        gridLayoutGroup.padding.left = gridLayoutGroup.padding.right = (int)space / 5;
        gridLayoutGroup.spacing = new Vector2((float)space / 5, gridLayoutGroup.spacing.y);
    }

    public void BuildPlayerInfoList()
    {
        ClearPlayerInfoList();

        if (FirebaseManager.Instance == null || FirebaseManager.Instance.m_PlayerCardList == null)
            return;

        BuildTopPlayerList();

        int cardIndex = 0;

        foreach (PlayerProfile card in FirebaseManager.Instance.m_PlayerCardList)
        {
            AddPlayerCard(cardIndex, card);
            cardIndex++;
        }

        UpdatePlayerInfoListLayout();
    }

    public void BuildTopPlayerList()
    {
        ClearTopPlayerList();

        for (int i = 0; i < FirebaseManager.Instance.m_PlayerCardList.Count; i++)
        {
            if (i == 10)
                break;

            if (PlayerPrefs.GetString("UserID", string.Empty) == FirebaseManager.Instance.m_PlayerCardList[i].UserID)
            {
                continue;
            }

            GameObject topPlayer = Instantiate(m_RightPanel_TopPlayer_Prefab);
            topPlayer.transform.SetParent(m_RightPanel_TopPlayer_List, false);
            topPlayer.name = FirebaseManager.Instance.m_PlayerCardList[i].Name;
            SetPlayerImage(topPlayer.transform.GetChild(1).GetChild(0).GetComponent<RawImage>(), FirebaseManager.Instance.m_PlayerCardList[i].UserID);
            topPlayer.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = FirebaseManager.Instance.m_PlayerCardList[i].Name;
            topPlayer.transform.GetChild(3).gameObject.SetActive(FirebaseManager.Instance.m_PlayerCardList[i].IsMVP);
            topPlayer.transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().text 
                = FirebaseManager.Instance.m_PlayerCardList[i].Badges == 0 ? "Not Available" : FirebaseManager.Instance.m_PlayerCardList[i].Badges.ToString();
        }
    }

    private void SetPlayerImage(RawImage image, string userId)
    {
#if UNITY_ANDROID
        string imagePath = Application.persistentDataPath + "/Profiles/" + userId + ".jpg";
#elif UNITY_IOS
        string imagePath = Application.persistentDataPath + "/Profiles/" + userId + ".jpg";
#endif
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

        Texture2D texture = new Texture2D(296, 370);
        texture.LoadImage(imageBytes);
        float aspectRatio = (float)texture.width / texture.height;
        image.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        image.GetComponent<AspectRatioFitter>().aspectRatio = aspectRatio;
        image.texture = texture;
    }

    private void ClearTopPlayerList()
    {
        foreach (Transform topPlayer in m_RightPanel_TopPlayer_List)
        {
            Destroy(topPlayer.gameObject);
        }

        m_RightPanel_TopPlayer_List.DetachChildren();
    }

    private void ClearPlayerInfoList()
    {
        foreach (Transform cardTrans in m_PlayerInfoListTransform)
        {
            Destroy(cardTrans.gameObject);
        }

        m_PlayerInfoListTransform.DetachChildren();
    }

    private void AddPlayerCard(int cardIndex, PlayerProfile playerCard)
    {
        string userId = PlayerPrefs.GetString("UserID", string.Empty);
        if (userId == playerCard.UserID)
        {
            return;
        }

        GameObject card = Instantiate(m_PlayerCardPrefab);
        card.transform.SetParent(m_PlayerInfoListTransform, false);
        card.name = playerCard.Name;
        ColorUtility.TryParseHtmlString("#" + playerCard.CardTopColor, out Color topColor);
        card.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = topColor;
        card.transform.GetChild(1).GetChild(0).GetComponent<Image>().maskable = true;
        card.transform.GetChild(1).GetChild(0).GetComponent<Image>().enabled = false;
        card.transform.GetChild(1).GetChild(0).GetComponent<Image>().enabled = true;
        ColorUtility.TryParseHtmlString("#" + playerCard.CardBottomColor, out Color bottomColor);
        card.transform.GetChild(1).GetChild(1).GetComponent<Image>().color = bottomColor;
        card.transform.GetChild(1).GetChild(1).GetComponent<Image>().maskable = true;
        card.transform.GetChild(1).GetChild(1).GetComponent<Image>().enabled = false;
        card.transform.GetChild(1).GetChild(1).GetComponent<Image>().enabled = true;
        card.transform.GetChild(4).GetComponent<TextMeshProUGUI>().text = playerCard.Name;
        card.transform.GetChild(4).GetComponent<TextMeshProUGUI>().color = SetInvertedColor(topColor);
        card.transform.GetChild(5).GetComponent<TextMeshProUGUI>().text = playerCard.Rank == 0 ? "" : "RANK " + playerCard.Rank;
        card.transform.GetChild(6).GetComponent<TextMeshProUGUI>().text = playerCard.TeamPosition;
        card.transform.GetChild(6).GetComponent<TextMeshProUGUI>().color = SetInvertedColor(bottomColor);
        if (playerCard.Image.Length > 0)
            SetPlayerImage(card.transform.GetChild(3).GetChild(0).GetChild(0).GetComponent<RawImage>(), playerCard.UserID);
    }

    private Color SetInvertedColor(Color original)
    {
        Color inverted = new Color(1 - original.r, 1 - original.g, 1 - original.b);
        if (inverted.r >= 0.45f && inverted.r <= 0.55f
            && inverted.g >= 0.45f && inverted.g <= 0.55f
            && inverted.b >= 0.45f && inverted.b <= 0.55f)
        {
            inverted = Color.black;
        }

        return inverted;
    }

    private void UpdatePlayerInfoListLayout()
    {
        StartCoroutine(ApplyScrollPosition());
    }

    private IEnumerator ApplyScrollPosition()
    {
        yield return new WaitForEndOfFrame();
        m_PlayerInfoScrollRect.horizontalNormalizedPosition = 0;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_PlayerInfoScrollRect.transform);
    }
}
