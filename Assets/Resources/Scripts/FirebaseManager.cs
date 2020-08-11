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
using System.Linq;
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
    public FirebaseFirestore firestore;
    public FirebaseStorage storage;

    [HideInInspector] public List<Court> m_CourtList;
    [HideInInspector] public List<PlayerProfile> m_PlayerCardList;

    [HideInInspector] public bool m_IsCourtsDownloadProgressCompleted = false;
    [HideInInspector] public bool m_IsProfilesDownloadProgressCompleted = false;

    private List<bool> m_CourtsListDownloaded;
    private List<bool> m_ProfilesListDownloaded;
    private int m_CourtsListCount = 0;
    private int m_ProfilesListCount = 0;

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

    private async void Start()
    {
        m_CourtList = new List<Court>();
        m_PlayerCardList = new List<PlayerProfile>();

        m_CourtsListDownloaded = new List<bool>();
        m_ProfilesListDownloaded = new List<bool>();

#if UNITY_EDITOR
        await InitializeFirebaseAsync();
#else
        await FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(async task =>
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
    }

    private async Task InitializeFirebaseAsync()
    {
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);

        if (!FB.IsInitialized)
        {
            FB.Init(InitCallback, OnHideUnity);
        }
        else
        {
            FB.ActivateApp();
        }

        firestore = FirebaseFirestore.DefaultInstance;
        storage = FirebaseStorage.DefaultInstance;

        await FetchCourtsAsync();
        await FetchPlayersInfoAsync();
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
        if (firestore != null)
        {
            firestore.TerminateAsync();
            firestore.ClearPersistenceAsync();
        }
        Instance = null;
    }

    private void OnApplicationQuit()
    {
        FirebaseApp.DefaultInstance.Dispose();
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

    private void OnGoogleAuthenticationFinished(Task<GoogleSignInUser> task)
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
            auth.SignInWithCredentialAsync(credential).ContinueWith(t =>
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

                StartCoroutine(Profile.Instance.GetProfileAsync());

                Main.Instance.HideLoginLoadingBar();
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

        await auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
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

            StartCoroutine(Profile.Instance.GetProfileAsync());
        });

        Main.Instance.HideLoginLoadingBar();
    }

    private void InitCallback()
    {
        if (FB.IsLoggedIn)
        {
            if (PlayerPrefs.GetInt("IsLoggedIn", 0) == 1
                && PlayerPrefs.GetString("SocialPlatform", "Google") == "Facebook"
                && PlayerPrefs.GetString("UserID", string.Empty) != string.Empty)
            {
                _ = FacebookAuth(AccessToken.CurrentAccessToken.TokenString);
            }
            else
            {
                if (Main.Instance != null) Main.Instance.HideLoginLoadingBar();
                Debug.Log("User not yet loged FB or loged out");
            }
        }
        else
        { 
            if (Main.Instance != null) Main.Instance.HideLoginLoadingBar();
            Debug.Log("User cancelled login");
        }
    }

    private void OnHideUnity(bool isGameShown)
    {
        if (!isGameShown)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
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
                Debug.Log("FetchCourtsAsync() -> query.GetSnapshotAsync() was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.Log("FetchCourtsAsync() -> query.GetSnapshotAsync() encountered an error: " + task.Exception);
                return;
            }

            QuerySnapshot allCourtsSnapshot = task.Result;

            m_CourtsListCount = allCourtsSnapshot.Documents.Count();

            foreach (DocumentSnapshot documentSnapshot in allCourtsSnapshot.Documents)
            {
                Dictionary<string, object> court = documentSnapshot.ToDictionary();
                StorageReference imageReference = storage.GetReference(court["Image"].ToString());

                string imagePath = Application.persistentDataPath + "/" + court["Image"].ToString();

                bool canParse = int.TryParse(court["ID"].ToString(), out int imageID);
                if (canParse == false) continue;

                bool isImageDownloaded = File.Exists(imagePath);
                if (!isImageDownloaded)
                {
                    m_CourtsListDownloaded.Add(false);
                    _ = DownloadCourtImageAsync(imageReference, imagePath, imageID);
                }
                else
                {
                    m_CourtsListDownloaded.Add(true);
                }

                m_CourtList.Add(new Court(imageID, court["Name"].ToString(), court["Address"].ToString(), imagePath));

                if (m_CourtsListDownloaded.Count == m_CourtsListCount)
                    VerifyCourtsListDownloadProgressCompleted();
            }
        });
    }

    private async Task DownloadCourtImageAsync(StorageReference imageReference, string path, int id)
    {
        await imageReference.GetFileAsync(path).ContinueWith(resultTask =>
        {
            if (!resultTask.IsFaulted && !resultTask.IsCanceled)
            {
                if (Courts.Instance != null)
                {
                    Courts.Instance.SetCourtImage(id, path);
                }
            }

            m_CourtsListDownloaded[id - 1] = true;
            if (m_CourtsListDownloaded.Count == m_CourtsListCount)
                VerifyCourtsListDownloadProgressCompleted();
        });
    }

    private void VerifyCourtsListDownloadProgressCompleted()
    {
        for (int i = 0; i < m_CourtsListDownloaded.Count; i ++)
        {
            if (m_CourtsListDownloaded[i] == false)
                return;
        }

        m_IsCourtsDownloadProgressCompleted = true;
    }
    #endregion

    #region Players Information
    private async Task FetchPlayersInfoAsync()
    {
        CollectionReference playersRef = firestore.Collection("Profiles");
        Query query = playersRef.OrderBy("Name");

        string persistentCourtsPath = Application.persistentDataPath + "/Profiles";

        if (!Directory.Exists(persistentCourtsPath))
        {
            Directory.CreateDirectory(persistentCourtsPath);
        }

        await query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("FetchPlayersInfoAsync() -> query.GetSnapshotAsync() was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.Log("FetchPlayersInfoAsync() -> query.GetSnapshotAsync() encountered an error: " + task.Exception);
                return;
            }

            QuerySnapshot allPlayersSnapshot = task.Result;

            m_ProfilesListCount = allPlayersSnapshot.Documents.Count();

            if (m_ProfilesListCount == 0)
            {
                m_IsProfilesDownloadProgressCompleted = true;
                return;
            }

            int playerIndex = 0;

            foreach (DocumentSnapshot documentSnapshot in allPlayersSnapshot.Documents)
            {
                Dictionary<string, object> player = documentSnapshot.ToDictionary();
                
                string imagePath = "";

                if (player["Image"].ToString().Length > 0 && player["Image"].ToString().Contains("Profiles/"))
                {
                    StorageReference imageReference = storage.GetReference(player["Image"].ToString());

                    imagePath = Application.persistentDataPath + "/" + player["Image"].ToString();

                    bool isImageDownloaded = File.Exists(imagePath);
                    if (!isImageDownloaded)
                    {
                        m_ProfilesListDownloaded.Add(false);
                        _ = DownloadPlayerImageAsync(imageReference, imagePath, playerIndex);
                    }
                    else
                    {
                        m_ProfilesListDownloaded.Add(true);
                    }
                }
                else
                {
                    m_ProfilesListDownloaded.Add(true);
                }

                int.TryParse(player["Rank"].ToString(), out int rank);
                int.TryParse(player["Badges"].ToString(), out int badges);
                bool.TryParse(player["IsMVP"].ToString(), out bool isMVP);

                m_PlayerCardList.Add(new PlayerProfile(player["UserID"].ToString(), player["Name"].ToString(), imagePath, rank, badges, 
                    isMVP, null, player["TeamPosition"].ToString(), player["CardTopColor"].ToString(), player["CardBottomColor"].ToString()));

                playerIndex++;

                if (m_ProfilesListDownloaded.Count == m_ProfilesListCount)
                    VerifyProfilesListDownloadProgressCompleted();
            }
        });
    }

    private async Task DownloadPlayerImageAsync(StorageReference imageReference, string path, int id)
    {
        await imageReference.GetFileAsync(path).ContinueWith(resultTask =>
        {
            if (!resultTask.IsFaulted && !resultTask.IsCanceled)
            {
                if (PlayerInfo.Instance != null)
                {
                    PlayerInfo.Instance.SetPlayerImage(id, path);
                }
            }

            m_ProfilesListDownloaded[id] = true;
            if (m_ProfilesListDownloaded.Count == m_ProfilesListCount)
                VerifyProfilesListDownloadProgressCompleted();
        });
    }

    private void VerifyProfilesListDownloadProgressCompleted()
    {
        for (int i = 0; i < m_ProfilesListDownloaded.Count; i++)
        {
            if (m_ProfilesListDownloaded[i] == false)
                return;
        }

        m_IsProfilesDownloadProgressCompleted = true;
    }
    #endregion

    public void SaveProfile()
    {
        _ = Profile.Instance.SaveProfileAsync(user, firestore);
    }

    public void DeleteAccount()
    {
        _ = Profile.Instance.DeleteAccountAsync(user, auth, firestore);
    }
}
