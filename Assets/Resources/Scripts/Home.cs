using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Home : MonoBehaviour
{
    public static Home Instance;

    [SerializeField] private GameObject m_CourtCardPrefab;
    [SerializeField] private ScrollRect m_CourtScrollRect;
    [SerializeField] private Transform m_CourtListTransform;

    private void Awake()
    {
        Instance = this;
    }

    public void ClearCourtsList()
    {
        foreach (Transform cardTrans in m_CourtListTransform)
        {
            Destroy(cardTrans.gameObject);
        }

        m_CourtListTransform.DetachChildren();
    }

    public void AddCourt(int id, string name, string address, string imagePath = "")
    {
        GameObject card = Instantiate(m_CourtCardPrefab);
        card.transform.SetParent(m_CourtListTransform, false);
        card.name = name;
        card.transform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = name;
        card.transform.GetChild(1).GetChild(2).GetComponent<TextMeshProUGUI>().text = address;
        if (imagePath.Length > 0)
            SetCourtImage(id, imagePath);
    }

    public void UpdateCourt()
    {

    }

    public void UpdateCourtImageDownloadProgress(int id, int percentage)
    {
        GameObject loaderSlider = m_CourtListTransform.GetChild(id - 1).GetChild(1).GetChild(0).GetChild(1).gameObject;
        loaderSlider.SetActive(true);
        loaderSlider.transform.GetChild(0).GetComponent<Slider>().value = percentage;
        loaderSlider.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = "Downloading: " + percentage + "%";
    }

    public void SetCourtImage(int id, string imagePath)
    {
        GameObject loaderSlider = m_CourtListTransform.GetChild(id - 1).GetChild(1).GetChild(0).GetChild(1).gameObject;
        loaderSlider.SetActive(false);

        FileInfo fileInfo = new FileInfo(imagePath);
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

    public void UpdateCourtListLayout()
    {
        StartCoroutine(ApplyScrollPosition());
    }

    private IEnumerator ApplyScrollPosition()
    {
        yield return new WaitForEndOfFrame();
        m_CourtScrollRect.horizontalNormalizedPosition = 0;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_CourtScrollRect.transform);
    }
}
