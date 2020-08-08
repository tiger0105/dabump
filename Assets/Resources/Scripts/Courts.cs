using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.Storage;
using System;
using System.Collections.Generic;
using System.Threading;
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

        await query.GetSnapshotAsync().ContinueWithOnMainThread(async task =>
        {
            if (task.IsCanceled)
            {
                DebugLog("query.GetSnapshotAsync() was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                DebugLog("query.GetSnapshotAsync() encountered an error: " + task.Exception);
                return;
            }

            QuerySnapshot allCourtsSnapshot = task.Result;

            foreach (DocumentSnapshot documentSnapshot in allCourtsSnapshot.Documents)
            {
                Dictionary<string, object> court = documentSnapshot.ToDictionary();
                StorageReference imageReference = storage.GetReference(court["Image"].ToString());

                Task imageTask = imageReference.GetFileAsync
                (
                    Application.persistentDataPath + "/" + court["Image"].ToString(), 
                    new StorageProgress<DownloadState>((DownloadState state) => 
                    {
                        DebugLog(string.Format(
                            "Progress: {0} of {1} bytes transferred.",
                            state.BytesTransferred,
                            state.TotalByteCount
                        ));
                    }), 
                    CancellationToken.None
                );

                await imageTask.ContinueWith(resultTask => 
                {
                    if (!resultTask.IsFaulted && !resultTask.IsCanceled)
                    {
                        Debug.Log("Download finished.");
                    }
                });
                //DebugLog(court["ID"].ToString());
                DebugLog(court["Image"].ToString());
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
