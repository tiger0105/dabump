using Facebook.Unity;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.Storage;
using Google;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Query = Firebase.Firestore.Query;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;

    private readonly string webClientId = "1013499565612-oto0j58l915sbmcplota9a3evbpugev2.apps.googleusercontent.com";    
    private GoogleSignInConfiguration configuration;
#if !UNITY_EDITOR
    private DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
#endif
    private FirebaseAuth auth;
    private FirebaseUser user;
    private FirebaseFirestore firestore;
    private FirebaseStorage storage;

    [HideInInspector] public List<Court> m_CourtList;

    private void Awake()
    {
        Instance = this;

        configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestIdToken = true, 
            RequestEmail = true,
            RequestProfile = true
        };

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
#if UNITY_EDITOR
        _ = InitializeFirebaseAsync();
#else
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(async task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                await InitializeFirebaseAsync();
            }
            else
            {
                Debug.Log("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
#endif
        m_CourtList = new List<Court>();
    }

    private async Task InitializeFirebaseAsync()
    {
        FirebaseApp app = FirebaseApp.Create();
        auth = FirebaseAuth.GetAuth(app);
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);

        if (FB.IsInitialized)
        {
            FB.ActivateApp();
        }

        firestore = FirebaseFirestore.GetInstance(app);
        storage = FirebaseStorage.GetInstance(app);

        await FetchCourtsAsync();
    }

    private void AuthStateChanged(object sender, EventArgs eventArgs)
    {
    }

    private void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
            auth.SignOut();
        }
        auth = null;
        Instance = null;
    }

    private void OnApplicationQuit()
    {
        if (firestore != null)
        {
            firestore.TerminateAsync();
            firestore.ClearPersistenceAsync();
        }
    }

    #region Google SignIn
    public void OnGoogleSignIn()
    {
        Main.Instance.ShowLoginLoadingBar();

        GoogleSignIn.Configuration = configuration;
#if UNITY_ANDROID && !UNITY_EDITOR
        GoogleSignIn.Configuration.UseGameSignIn = false;
#endif
        GoogleSignIn.Configuration.RequestIdToken = true;

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnGoogleAuthenticationFinished);
    }

    private async Task OnGoogleAuthenticationFinished(Task<GoogleSignInUser> task)
    {
        if (task.IsFaulted)
        {
            Debug.Log("OnGoogleAuthenticationFinished task.IsFaulted");
            Main.Instance.HideLoginLoadingBar();
        }
        else if (task.IsCanceled)
        {
            Main.Instance.HideLoginLoadingBar();
            Debug.Log("OnGoogleAuthenticationFinished task.IsCanceled");
        }
        else
        {
            Credential credential = GoogleAuthProvider.GetCredential(task.Result.IdToken, null);
            await auth.SignInWithCredentialAsync(credential).ContinueWith(async t =>
            {
                if (t.IsCanceled)
                {
                    Main.Instance.HideLoginLoadingBar();
                    Debug.Log("OnGoogleAuthenticationFinished t.IsCanceled");
                    return;
                }

                if (t.IsFaulted)
                {
                    Main.Instance.HideLoginLoadingBar();
                    Debug.Log("OnGoogleAuthenticationFinished t.IsFaulted");
                    return;
                }

                user = auth.CurrentUser;

                PlayerPrefs.SetString("UserID", user.UserId);
                PlayerPrefs.SetString("UserName", user.DisplayName);
                PlayerPrefs.SetString("UserEmail", user.Email);
                PlayerPrefs.SetString("SocialPlatform", "Google");
                PlayerPrefs.SetInt("IsLoggedIn", 1);

                await Profile.Instance.GetProfileAsync(firestore);

                Main.Instance.SwitchToMainPanel();
                return;
            });

            Main.Instance.HideLoginLoadingBar();
        }
    }
#endregion

    #region Facebook SignIn
    public void OnFacebookSignIn()
    {
        Main.Instance.ShowLoginLoadingBar();

        FB.LogInWithReadPermissions(new List<string>() { "public_profile", "email" }, OnFacebookAuthenticationFinished);
    }

    private void OnFacebookAuthenticationFinished(IResult result)
    {
        if (FB.IsLoggedIn)
        {
            _ = FacebookAuth(AccessToken.CurrentAccessToken.TokenString);
        }
        else
        {
            Main.Instance.HideLoginLoadingBar();
        }
    }

    private async Task FacebookAuth(string accessToken)
    {
        Credential credential = FacebookAuthProvider.GetCredential(accessToken);

        await auth.SignInWithCredentialAsync(credential).ContinueWith(async task =>
        {
            if (task.IsCanceled)
            {
                Main.Instance.HideLoginLoadingBar();
                Debug.Log("SignInWithCredentialAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Main.Instance.HideLoginLoadingBar();
                Debug.Log("SignInWithCredentialAsync encountered an error: " + task.Exception);
                return;
            }

            user = auth.CurrentUser;

            PlayerPrefs.SetString("UserID", user.UserId);
            PlayerPrefs.SetString("UserName", user.DisplayName);
            PlayerPrefs.SetString("UserEmail", user.Email);
            PlayerPrefs.SetString("SocialPlatform", "Facebook");
            PlayerPrefs.SetInt("IsLoggedIn", 1);

            await Profile.Instance.GetProfileAsync(firestore);

            Main.Instance.SwitchToMainPanel();
            return;
        });

        Main.Instance.HideLoginLoadingBar();
    }
    #endregion

    #region Courts
    private async Task FetchCourtsAsync()
    {
        CollectionReference courtsRef = firestore.Collection("Courts");
        Query query = courtsRef.OrderBy("ID");

        string persistentCourtsPath = Application.persistentDataPath + "/Courts";

        if (!Directory.Exists(persistentCourtsPath))
        {
            Directory.CreateDirectory(persistentCourtsPath);
        }

        await query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("GetCourts() -> query.GetSnapshotAsync() was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.Log("GetCourts() -> query.GetSnapshotAsync() encountered an error: " + task.Exception);
                return;
            }

            QuerySnapshot allCourtsSnapshot = task.Result;

            foreach (DocumentSnapshot documentSnapshot in allCourtsSnapshot.Documents)
            {
                Dictionary<string, object> court = documentSnapshot.ToDictionary();
                StorageReference imageReference = storage.GetReference(court["Image"].ToString());

                string imagePath = Application.persistentDataPath + "/" + court["Image"].ToString();

                bool canParse = int.TryParse(court["ID"].ToString(), out int imageID);
                if (canParse == false) continue;

                bool isImageDownloaded = File.Exists(imagePath);
                if (!isImageDownloaded) _ = DownloadImageAsync(imageReference, imagePath);

                m_CourtList.Add(new Court(imageID, court["Name"].ToString(), court["Address"].ToString(), imagePath));
            }
        });
    }

    private async Task DownloadImageAsync(StorageReference imageReference, string path)
    {
        await imageReference.GetFileAsync(path);
    }
    #endregion
}
