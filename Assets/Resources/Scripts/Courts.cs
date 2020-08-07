using Firebase.Firestore;
using System.Collections.Generic;
using UnityEngine;

public class Courts : MonoBehaviour
{
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
                Debug.Log(court["ID"]);
                Debug.Log(court["Image"]);
                Debug.Log(court["Name"]);
                Debug.Log(court["Address"]);
            }
        });
    }
}
