using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    private void Start()
    {
        GetCourts();
    }

    public async void GetCourts()
    {
        m_CourtsLoadingBar.SetActive(true);

        FirebaseFirestore firestore = FirebaseFirestore.DefaultInstance;
        FirebaseStorage storage = FirebaseStorage.DefaultInstance;

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
                StorageReference imageReference = storage.GetReference(court["Image"].ToString());
                imageReference.GetDownloadUrlAsync().ContinueWith((Task<Uri> taskUri) => 
                {
                    if (!taskUri.IsFaulted && !taskUri.IsCanceled)
                    {
                        DebugLog("Download URL: " + taskUri.Result.AbsoluteUri);
                    }
                });
                //DebugLog(court["ID"].ToString());
                //DebugLog(court["Image"].ToString());
                //DebugLog(court["Name"].ToString());
                //DebugLog(court["Address"].ToString());
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
