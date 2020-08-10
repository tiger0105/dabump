using DigitalRubyShared;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class Home : MonoBehaviour
{
    public static Home Instance;

    [Header("Recently Visited Courts Section")]
    [SerializeField] private GameObject m_CourtCardPrefab;
    [SerializeField] private ScrollRect m_CourtScrollRect;
    [SerializeField] private Transform m_CourtListTransform;
    [Header("Get Started Section")]
    [SerializeField] public HorizontalScrollSnap m_HorizontalScrollSnap;
    [SerializeField] public Transform m_NavigationBar;

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

    public void Navigate(bool isOn)
    {
        if (isOn == true)
        {
            m_HorizontalScrollSnap.GoToScreen(GetComponent<ToggleGroup>().ActiveToggles().First().transform.GetSiblingIndex());
        }
    }

    public void NavigatePage()
    {
        m_NavigationBar.GetChild(m_HorizontalScrollSnap._currentPage).GetComponent<Toggle>().isOn = true;
    }
}
