using Firebase.Firestore;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Courts : MonoBehaviour
{
    public TextMeshProUGUI debugText;

    public static Courts Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        
    }

    public void GetCourts()
    {
        FirebaseFirestore firestore = FirebaseFirestore.GetInstance(Firebase.FirebaseApp.DefaultInstance);

        CollectionReference courtsRef = firestore.Collection("Courts");
        Query query = courtsRef.OrderByDescending("ID");

        ListenerRegistration listener = query.Listen(snapshot =>
        {
            foreach (DocumentSnapshot documentSnapshot in snapshot.Documents)
            {
                Dictionary<string, object> court = documentSnapshot.ToDictionary();
                DebugLog(court["ID"].ToString());
                DebugLog(court["Image"].ToString());
                DebugLog(court["Name"].ToString());
                DebugLog(court["Address"].ToString());
            }
        });
    }

    private void DebugLog(string text)
    {
        Debug.Log(text);
        debugText.text += text + "\n";
    }
}
