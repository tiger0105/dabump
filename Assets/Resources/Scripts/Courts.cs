using Firebase.Extensions;
using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        card.transform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = name;
        card.transform.GetChild(1).GetChild(2).GetComponent<TextMeshProUGUI>().text = address;
        //string playersList = string.Empty;
        //if (checkedInPlayers == null)
        //    playersList = "No players available";
        //else
        //    playersList = checkedInPlayers.Aggregate((a, x) => a + ", " + x).ToString();
        //card.transform.GetChild(1).GetChild(3).GetComponent<TextMeshProUGUI>().text = playersList;

        SetCourtImage(id, imagePath);
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
