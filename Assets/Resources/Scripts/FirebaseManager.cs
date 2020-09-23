using AppleAuth.IOS;
using AppleAuth.IOS.Enums;
using AppleAuth.IOS.Interfaces;
using AppleAuth.IOS.NativeMessages;
using Facebook.Unity;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.Storage;
using Google;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
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

    private IAppleAuthManager _appleAuthManager;
    private OnDemandMessageHandlerScheduler _scheduler;

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

    private void Start()
    {
        //PlayerPrefs.SetString("UserID", "RHxbh06ZuVSB0vgHjnJ4q8iLamm2");

        m_CourtList = new List<Court>();
        m_PlayerCardList = new List<PlayerProfile>();

        m_CourtsListDownloaded = new List<bool>();
        m_ProfilesListDownloaded = new List<bool>();

#if UNITY_EDITOR
        InitializeFirebaseAndFetchData();
#else
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebaseAndFetchData();
            }
            else
            {
                Debug.Log("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
#endif

        _scheduler = new OnDemandMessageHandlerScheduler();
        var deserializer = new PayloadDeserializer();
        _appleAuthManager = new AppleAuthManager(deserializer, _scheduler);
    }

    private void Update()
    {
        if (_scheduler != null)
            _scheduler.Update();
    }

    private void InitializeFirebaseAndFetchData()
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

        _ =  FetchCourtsAsync();
        _ = FetchPlayersInfoAsync();
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
        FirebaseApp.DefaultInstance.Dispose();
    }

    #region Google SignIn
    public void OnGoogleSignIn()
    {
#if UNITY_EDITOR
        StartCoroutine(Profile.Instance.GetProfileAsync());
        return;
#endif

        GoogleSignIn.Configuration = configuration;
#if UNITY_ANDROID && !UNITY_EDITOR
        GoogleSignIn.Configuration.UseGameSignIn = false;
#endif
        GoogleSignIn.Configuration.RequestIdToken = true;

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnGoogleAuthenticationFinished);
    }

    private void OnGoogleAuthenticationFinished(Task<GoogleSignInUser> task)
    {
        if (task.IsCanceled)
        {
            Main.Instance.HideLoginLoadingBar();
            return;
        }

        
        if (task.IsFaulted)
        {
            Main.Instance.HideLoginLoadingBar();
            return;
        }

        Credential credential = GoogleAuthProvider.GetCredential(task.Result.IdToken, null);
        auth.SignInWithCredentialAsync(credential).ContinueWith(t =>
        {
            if (t.IsCanceled)
            {
                Main.Instance.HideLoginLoadingBar();
                return;
            }

            if (t.IsFaulted)
            {
                Main.Instance.HideLoginLoadingBar();
                return;
            }

            Main.Instance.ShowLoginLoadingBar();

            user = auth.CurrentUser;

            PlayerPrefs.SetString("UserID", user.UserId);
            PlayerPrefs.SetString("UserName", user.DisplayName);
            PlayerPrefs.SetString("UserEmail", user.Email);
            PlayerPrefs.SetString("SocialPlatform", "Google");
            PlayerPrefs.SetInt("IsLoggedIn", 1);

            StartCoroutine(Profile.Instance.GetProfileAsync());
        });
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
        if (result.Cancelled)
        {
            Main.Instance.HideLoginLoadingBar();
            return;
        }

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
        Main.Instance.ShowLoginLoadingBar();

        Credential credential = FacebookAuthProvider.GetCredential(accessToken);

        await auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Main.Instance.HideLoginLoadingBar();
                return;
            }
            if (task.IsFaulted)
            {
                Main.Instance.HideLoginLoadingBar();
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
            }
        }
        else
        { 
            if (Main.Instance != null) Main.Instance.HideLoginLoadingBar();
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

#region Apple SignIn
    public void OnAppleSignIn()
    {
        Main.Instance.ShowLoginLoadingBar();

        _appleAuthManager.LoginWithAppleId(
            LoginOptions.IncludeEmail | LoginOptions.IncludeFullName,
            credentials =>
            {
                if (!(credentials is IAppleIDCredential appleIdCredential))
                {
                    Main.Instance.HideLoginLoadingBar();
                    return;
                }

                string rawNonce = _appleAuthManager.RawNonce;

                string authCode = Encoding.UTF8.GetString(appleIdCredential.AuthorizationCode);

                Credential credential = OAuthProvider.GetCredential("apple.com", Encoding.UTF8.GetString(appleIdCredential.IdentityToken), rawNonce, authCode);

                auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
                {
                    if (task.IsCanceled)
                    {
                        Main.Instance.HideLoginLoadingBar();
                        return;
                    }


                    if (task.IsFaulted)
                    {
                        Main.Instance.HideLoginLoadingBar();
                        return;
                    }

                    user = auth.CurrentUser;

                    PlayerPrefs.SetString("UserID", user.UserId);
                    PlayerPrefs.SetString("UserName", user.DisplayName);
                    PlayerPrefs.SetString("UserEmail", user.Email);
                    PlayerPrefs.SetString("SocialPlatform", "Apple");
                    PlayerPrefs.SetInt("IsLoggedIn", 1);

                    StartCoroutine(Profile.Instance.GetProfileAsync());
                });
            },
            error =>
            {
                Main.Instance.HideLoginLoadingBar();
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
#if UNITY_ANDROID
        string persistentCourtsPath = Application.persistentDataPath + "/Courts";
#elif UNITY_IOS
        string persistentCourtsPath = Application.persistentDataPath + "/Courts";
#endif

        if (!Directory.Exists(persistentCourtsPath))
        {
            Directory.CreateDirectory(persistentCourtsPath);
        }

        await query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                return;
            }
            
            if (task.IsFaulted)
            {
                return;
            }

            QuerySnapshot allCourtsSnapshot = task.Result;

            m_CourtsListCount = allCourtsSnapshot.Documents.Count();

            foreach (DocumentSnapshot documentSnapshot in allCourtsSnapshot.Documents)
            {
                Dictionary<string, object> court = documentSnapshot.ToDictionary();
                StorageReference imageReference = storage.GetReference(court["Image"].ToString());
#if UNITY_ANDROID
                string imagePath = Application.persistentDataPath + "/" + court["Image"].ToString();
#elif UNITY_IOS
                string imagePath = Application.persistentDataPath + "/" + court["Image"].ToString();
#endif
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

                GeoPoint location = (GeoPoint)court["Location"];

                m_CourtList.Add(new Court(location, imageID, court["Name"].ToString(), court["Address"].ToString(), imagePath, null, court["Url"].ToString(), court["Type"].ToString()));

                if (m_CourtsListDownloaded.Count == m_CourtsListCount)
                    VerifyCourtsListDownloadProgressCompleted();
            }
        });
    }

    private async Task DownloadCourtImageAsync(StorageReference imageReference, string path, int id)
    {
#if UNITY_ANDROID
        await imageReference.GetFileAsync(path).ContinueWith(resultTask =>
#elif UNITY_IOS
        await imageReference.GetFileAsync("file://" + path).ContinueWith(resultTask =>
#endif
        {
            if (resultTask.IsCanceled)
            {
                Debug.Log("DownloadCourtImageAsync() -> imageReference.GetFileAsync() was canceled.");
            }
            else if (resultTask.IsFaulted)
            {
                Debug.Log("DownloadCourtImageAsync() -> imageReference.GetFileAsync() encountered an error: " + resultTask.Exception);
            }
            else
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
        Query query = playersRef.OrderByDescending("Badges");
#if UNITY_ANDROID
        string persistentCourtsPath = Application.persistentDataPath + "/Profiles";
#elif UNITY_IOS
        string persistentCourtsPath = Application.persistentDataPath + "/Profiles";
#endif
        if (!Directory.Exists(persistentCourtsPath))
        {
            Directory.CreateDirectory(persistentCourtsPath);
        }

        await query.GetSnapshotAsync().ContinueWithOnMainThread(async task =>
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
#if UNITY_ANDROID
                    imagePath = Application.persistentDataPath + "/" + player["Image"].ToString();
#elif UNITY_IOS
                    imagePath = Application.persistentDataPath + "/" + player["Image"].ToString();
#endif
                    bool isImageDownloaded = false;
                    FileInfo info = new FileInfo(imagePath);
                    if (info.Exists)
                    {
                        StorageMetadata meta = await imageReference.GetMetadataAsync();
                        if (meta.SizeBytes == info.Length)
                            isImageDownloaded = true;
                    }
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

                int.TryParse(player["Badges"].ToString(), out int badges);
                int.TryParse(player["CheckedInCourt"].ToString(), out int checkedInCourt);

                Debug.Log(player["UserID"].ToString());

                m_PlayerCardList.Add(new PlayerProfile(player["UserID"].ToString(), player["Name"].ToString(), imagePath, playerIndex + 1, badges,
                    playerIndex == 0, checkedInCourt, player["VisitedCourts"].ToString(), player["Status"].ToString(), player["TeamPosition"].ToString(), player["CardTopColor"].ToString(), player["CardBottomColor"].ToString()));

                playerIndex++;

                if (m_ProfilesListDownloaded.Count == m_ProfilesListCount)
                    VerifyProfilesListDownloadProgressCompleted();
            }
        });
    }

    private async Task DownloadPlayerImageAsync(StorageReference imageReference, string path, int id)
    {
#if UNITY_ANDROID
        await imageReference.GetFileAsync(path).ContinueWith(resultTask =>
#elif UNITY_IOS
        await imageReference.GetFileAsync("file://" + path).ContinueWith(resultTask =>
#endif
        {
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

    public async Task UpdateDisplayName(string name)
    {
        if (user != null)
        {
            UserProfile userProfile = new UserProfile
            {
                DisplayName = name
            };

            await user.UpdateUserProfileAsync(userProfile).ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    Debug.Log("OnGoogleAuthenticationFinished t.IsCanceled");
                    return;
                }

                if (t.IsFaulted)
                {
                    Debug.Log("OnGoogleAuthenticationFinished t.IsFaulted");
                    return;
                }

                Debug.Log("Display name updated successfully");
            });

        }
    }

    public void SaveProfile(string temporaryPhotoPath)
    {
        _ = SaveProfileAsync(temporaryPhotoPath);
    }

    public async Task SaveProfileAsync(string temporaryPhotoPath)
    {
        Profile.Instance.m_LoadingBar.SetActive(true);

        string userId = PlayerPrefs.GetString("UserID", string.Empty);
        if (userId == string.Empty)
        {
            Profile.Instance.m_LoadingBar.SetActive(false);
            return;
        }

        await UpdateDisplayName(Profile.Instance.m_NameInputField.text);

        bool isImageUploaded = false;

        if (temporaryPhotoPath.Length > 0 
            && File.Exists(temporaryPhotoPath))
        {
            isImageUploaded = await UploadProfilePhotoAsync("file://" + temporaryPhotoPath, "Profiles/" + userId + ".jpg");
        }

        DocumentReference documentReference = firestore.Collection("Profiles").Document(userId);

        Dictionary<string, object> profile = new Dictionary<string, object>
        {
            ["Name"] = Profile.Instance.m_NameInputField.text,
            ["TeamPosition"] = Profile.Instance.m_CardPosition.text,
            ["CardTopColor"] = ColorUtility.ToHtmlStringRGB(Profile.Instance.m_CardTopColor.color),
            ["CardBottomColor"] = ColorUtility.ToHtmlStringRGB(Profile.Instance.m_CardBottomColor.color),
        };

        if (isImageUploaded == true)
            profile.Add("Image", "Profiles/" + userId + ".jpg");

        await documentReference.UpdateAsync(profile).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                if (isImageUploaded == true)
                {
#if UNITY_ANDROID
                    string fileName = Application.persistentDataPath + "/Profiles/" + userId + ".jpg";
#elif UNITY_IOS
                    string fileName = Application.persistentDataPath + "/Profiles/" + userId + ".jpg";
#endif
                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }
                }

                Profile.Instance.m_RightPanel_Name.text = Profile.Instance.m_NameInputField.text;
                Profile.Instance.m_RightPanel_TeamPosition.text = Profile.Instance.m_CardPosition.text;
            }
        });

        Profile.Instance.m_LoadingBar.SetActive(false);
    }

    public void DeleteAccount()
    {
        _ = Profile.Instance.DeleteAccountAsync(user, auth, firestore);
    }

    public async Task DeleteProfilePhotoAsync(string bucketUrl)
    {
        StorageReference imageReference = storage.GetReference(bucketUrl);
        await imageReference.DeleteAsync();
    }

    public async Task<bool> UploadProfilePhotoAsync(string fileName, string bucketUrl)
    {
        bool isUploaded = false;
        StorageReference imageReference = storage.GetReference(bucketUrl);
        await imageReference.PutFileAsync(fileName).ContinueWith((Task<StorageMetadata> task) =>
        {
            if (task.IsCompleted)
            {
                isUploaded = true;
            }
        });

        return isUploaded;
    }

    public async Task SetCheckInCourtAsync(PlayerProfile myProfile)
    {
        DocumentReference documentReference = firestore.Collection("Profiles").Document(myProfile.UserID);
        Dictionary<string, object> profile = new Dictionary<string, object>
        {
            ["CheckedInCourt"] = myProfile.CheckedInCourt,
            ["Badges"] = myProfile.Badges, 
            ["VisitedCourts"] = myProfile.VisitedCourts,
            ["Status"] = myProfile.Status,
        };

        await documentReference.UpdateAsync(profile);

        StartCoroutine(CancelCheckInStatus(myProfile.UserID));

        StartCoroutine(Courts.Instance.CourtCheckedIn(myProfile));
    }

    private IEnumerator CancelCheckInStatus(string userId)
    {
        WWWForm form = new WWWForm();
        form.AddField("userId", userId);

        using (UnityWebRequest www = UnityWebRequest.Post(AppData._REST_API_ENDPOINT + AppData._REST_API_CANCEL_CHECK_IN_STATUS, form))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                yield break;
            }
        }
    }

    public void DebugLog(string text)
    {
        GetComponentInChildren<TextMeshProUGUI>().text += text + "\n";
    }
}
