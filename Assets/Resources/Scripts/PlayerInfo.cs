using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfo : MonoBehaviour
{
    public static PlayerInfo Instance;

    [SerializeField] private GameObject m_PlayerCardPrefab;
    [SerializeField] private ScrollRect m_PlayerInfoScrollRect;
    [SerializeField] private Transform m_PlayerInfoListTransform;

    private void Awake()
    {
        Instance = this;
    }

    public void BuildPlayerInfoList()
    {
        ClearPlayerInfoList();

        if (FirebaseManager.Instance == null || FirebaseManager.Instance.m_PlayerCardList == null)
            return;

        int cardIndex = 0;

        foreach (PlayerCard card in FirebaseManager.Instance.m_PlayerCardList)
        {
            AddPlayerCard(cardIndex, card);
            cardIndex++;
        }

        UpdatePlayerInfoListLayout();
    }

    private void ClearPlayerInfoList()
    {
        foreach (Transform cardTrans in m_PlayerInfoListTransform)
        {
            Destroy(cardTrans.gameObject);
        }

        m_PlayerInfoListTransform.DetachChildren();
    }

    private void AddPlayerCard(int cardIndex, PlayerCard playerCard)
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
        ColorUtility.TryParseHtmlString("#" + playerCard.CardBottomColor, out Color bottomColor);
        card.transform.GetChild(1).GetChild(1).GetComponent<Image>().color = bottomColor;
        card.transform.GetChild(4).GetComponent<TextMeshProUGUI>().text = playerCard.Name;
        card.transform.GetChild(4).GetComponent<TextMeshProUGUI>().color = SetInvertedColor(topColor);
        card.transform.GetChild(5).GetComponent<TextMeshProUGUI>().text = playerCard.Rank == 0 ? "" : "RANK " + playerCard.Rank;
        card.transform.GetChild(6).GetComponent<TextMeshProUGUI>().text = playerCard.TeamPosition;
        card.transform.GetChild(6).GetComponent<TextMeshProUGUI>().color = SetInvertedColor(bottomColor);
        if (playerCard.ImagePath.Length > 0)
            SetPlayerImage(cardIndex, playerCard.ImagePath);
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

    public void SetPlayerImage(int id, string imagePath)
    {
        if (m_PlayerInfoListTransform.childCount <= id)
            return;

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

        Texture2D texture = new Texture2D(296, 370);
        texture.LoadImage(imageBytes);
        float aspectRatio = (float)texture.width / texture.height;
        m_PlayerInfoListTransform.GetChild(id).GetChild(3).GetChild(0).GetChild(0).GetComponent<AspectRatioFitter>().aspectRatio = aspectRatio;
        m_PlayerInfoListTransform.GetChild(id).GetChild(3).GetChild(0).GetChild(0).GetComponent<RawImage>().texture = texture;
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
