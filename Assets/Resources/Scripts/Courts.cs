using Firebase.Extensions;
using Firebase.Firestore;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Courts : MonoBehaviour
{
    [SerializeField] private GameObject m_CourtsLoadingBar;
    public TextMeshProUGUI debugText;

    public static Courts Instance;

    private void Awake()
    {
        Instance = this;
    }

    public async void GetCourts()
    {
        m_CourtsLoadingBar.SetActive(true);

        FirebaseFirestore firestore = FirebaseFirestore.GetInstance(Firebase.FirebaseApp.DefaultInstance);

        CollectionReference courtsRef = firestore.Collection("Courts");
        Query query = courtsRef.OrderBy("ID");

        await query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                DebugLog("GetCourts was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                DebugLog("GetCourts encountered an error: " + task.Exception);
                return;
            }

            QuerySnapshot allCourtsSnapshot = task.Result;

            foreach (DocumentSnapshot documentSnapshot in allCourtsSnapshot.Documents)
            {
                Dictionary<string, object> court = documentSnapshot.ToDictionary();
                DebugLog(court["ID"].ToString());
                DebugLog(court["Image"].ToString());
                DebugLog(court["Name"].ToString());
                DebugLog(court["Address"].ToString());
            }
        });

        m_CourtsLoadingBar.SetActive(false);
    }

    private void DebugLog(string text)
    {
        Debug.Log(text);
        debugText.text += text + "\n";
    }
}
