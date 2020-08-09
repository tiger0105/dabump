using Facebook.Unity;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.Storage;
using Google;
using Michsky.UI.Frost;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Query = Firebase.Firestore.Query;

public class SocialLoginManager : MonoBehaviour
{
    public static SocialLoginManager Instance;

    private readonly string webClientId = "1013499565612-oto0j58l915sbmcplota9a3evbpugev2.apps.googleusercontent.com";
    
    private GoogleSignInConfiguration configuration;
    private DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
    
    private FirebaseAuth auth;
    private FirebaseUser user;
    private FirebaseFirestore firestore;
    private FirebaseStorage storage;

    [Header("Login")]
    [SerializeField] private GameObject m_LoadingBar;
    [SerializeField] private GameObject m_SplashScreen;
    [SerializeField] private GameObject m_Background;
    [SerializeField] private GameObject m_MainPanel;
    [SerializeField] private GameObject m_MenuManager;
    //[Header("Courts")]
    //[SerializeField] private GameObject m_CourtsLoadingBar;
    [Header("Profile -> Profile Tab")]
    [SerializeField] private TMP_InputField m_ProfileNameInputField;
    [SerializeField] private GameObject m_SaveProfileLoadingBar;
    [Header("Debug")]
    public TextMeshProUGUI debugText;

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
    }

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(async task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                await InitializeFirebaseAsync();
            }
            else
            {
                DebugLog("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
    }

    private async Task InitializeFirebaseAsync()
    {
        FirebaseApp app = FirebaseApp.Create();
        auth = FirebaseAuth.GetAuth(app);
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

        firestore = FirebaseFirestore.GetInstance(app);
        storage = FirebaseStorage.GetInstance(app);

        await GetCourts();
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

    private void ShowLoginLoadingBar()
    {
        m_SplashScreen.GetComponent<Animator>().Play("Login to Loading");
    }

    private void HideLoginLoadingBar()
    {
        m_MenuManager.GetComponent<BlurManager>().BlurOutAnim();
        m_LoadingBar.GetComponent<CanvasGroup>().alpha = 0;
    }

    private void SwitchToMainPanel()
    {
        m_MenuManager.GetComponent<BlurManager>().BlurOutAnim();
        m_SplashScreen.GetComponent<Animator>().Play("Panel Out");
        m_MainPanel.GetComponent<Animator>().Play("Panel Start");
        m_Background.GetComponent<Animator>().Play("Switch");
        m_LoadingBar.GetComponent<CanvasGroup>().alpha = 0;
    }

    public void SwitchToLoginPanel()
    {
        m_MenuManager.GetComponent<BlurManager>().BlurInAnim();
        m_SplashScreen.GetComponent<Animator>().Play("Login");
        m_MainPanel.GetComponent<Animator>().Play("Wait");
        m_Background.GetComponent<Animator>().Play("Start");
        m_LoadingBar.GetComponent<CanvasGroup>().alpha = 0;
    }

    #region Google SignIn
    public void GoogleSignIn_Clicked()
    {
        OnGoogleSignIn();
    }

    private void OnGoogleSignIn()
    {
        ShowLoginLoadingBar();

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
            DebugLog("OnGoogleAuthenticationFinished task.IsFaulted");
            HideLoginLoadingBar();
        }
        else if (task.IsCanceled)
        {
            HideLoginLoadingBar();
            DebugLog("OnGoogleAuthenticationFinished task.IsCanceled");
        }
        else
        {
            Credential credential = GoogleAuthProvider.GetCredential(task.Result.IdToken, null);
            auth.SignInWithCredentialAsync(credential).ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    HideLoginLoadingBar();
                    DebugLog("OnGoogleAuthenticationFinished t.IsCanceled");
                    return;
                }

                if (t.IsFaulted)
                {
                    HideLoginLoadingBar();
                    DebugLog("OnGoogleAuthenticationFinished t.IsFaulted");
                    return;
                }

                user = auth.CurrentUser;

                PlayerPrefs.SetString("UserID", user.UserId);
                PlayerPrefs.SetString("UserName", user.DisplayName);
                PlayerPrefs.SetString("UserEmail", user.Email);
                PlayerPrefs.SetString("SocialPlatform", "Google");
                PlayerPrefs.SetInt("IsLoggedIn", 1);

                _ = GetProfile();
                AccountTab.Instance.LoadAccountTab();

                SwitchToMainPanel();
                return;
            });

            HideLoginLoadingBar();
        }
    }
    #endregion

    #region Facebook SignIn
    public void FacebookSignIn_Click()
    {
        OnFacebookSignIn();
    }

    private void OnFacebookSignIn()
    {
        ShowLoginLoadingBar();

        FB.LogInWithReadPermissions(new List<string>() { "public_profile", "email" }, OnFacebookAuthenticationFinished);
    }

    private void OnFacebookAuthenticationFinished(IResult result)
    {
        if (FB.IsLoggedIn)
        {
            FacebookAuth(AccessToken.CurrentAccessToken.TokenString);
        }
        else
        {
            HideLoginLoadingBar();
        }
    }

    private void FacebookAuth(string accessToken)
    {
        Credential credential = FacebookAuthProvider.GetCredential(accessToken);

        auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                HideLoginLoadingBar();
                DebugLog("SignInWithCredentialAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                HideLoginLoadingBar();
                DebugLog("SignInWithCredentialAsync encountered an error: " + task.Exception);
                return;
            }

            user = auth.CurrentUser;

            PlayerPrefs.SetString("UserID", user.UserId);
            PlayerPrefs.SetString("UserName", user.DisplayName);
            PlayerPrefs.SetString("UserEmail", user.Email);
            PlayerPrefs.SetString("SocialPlatform", "Facebook");
            PlayerPrefs.SetInt("IsLoggedIn", 1);

            _ = GetProfile();
            AccountTab.Instance.LoadAccountTab();

            SwitchToMainPanel();
            return;
        });

        HideLoginLoadingBar();
    }

    private void InitCallback()
    {
        if (FB.IsLoggedIn)
        {
            if (PlayerPrefs.GetInt("IsLoggedIn", 0) == 1
                && PlayerPrefs.GetString("SocialPlatform", "Google") == "Facebook"
                && PlayerPrefs.GetString("UserID", string.Empty) != string.Empty)
            {
                FacebookAuth(AccessToken.CurrentAccessToken.TokenString);
            }
            else
            {
                DebugLog("User not yet loged FB or loged out");
            }
        }
        else
        {
            DebugLog("User cancelled login");
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
                    DebugLog("OnGoogleAuthenticationFinished t.IsCanceled");
                    return;
                }

                if (t.IsFaulted)
                {
                    DebugLog("OnGoogleAuthenticationFinished t.IsFaulted");
                    return;
                }

                DebugLog("Display name updated successfully");
            });

        }
    }

    public async Task DeleteAccount()
    {
        if (user != null)
        {
            await user.DeleteAsync().ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    DebugLog("DeleteAsync was canceled.");
                    return;
                }

                if (task.IsFaulted)
                {
                    DebugLog("DeleteAsync encountered an error: " + task.Exception);
                    return;
                }

                if (auth != null)
                {
                    auth.SignOut();
                    SwitchToLoginPanel();
                }
            });
        }
    }

    private async Task GetCourts()
    {
        //m_CourtsLoadingBar.SetActive(true);

        DebugLog("GetCourts");

        CollectionReference courtsRef = firestore.Collection("Courts");
        Query query = courtsRef.OrderBy("ID");

        string persistentCourtsPath = Application.persistentDataPath + "/Courts";

        if (!Directory.Exists(persistentCourtsPath))
        {
            Directory.CreateDirectory(persistentCourtsPath);
        }

        Courts.Instance.ClearCourtsList();

        await query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                DebugLog("GetCourts() -> query.GetSnapshotAsync() was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                DebugLog("GetCourts() -> query.GetSnapshotAsync() encountered an error: " + task.Exception);
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
                if (!isImageDownloaded)
                {
                    _ = DownloadImageAsync(imageReference, imagePath, imageID);
                }

                Courts.Instance.AddCourt(imageID, court["Name"].ToString(), court["Address"].ToString(),  isImageDownloaded ? imagePath : "");
            }
        });

        //m_CourtsLoadingBar.SetActive(false);
    }

    private async Task DownloadImageAsync(StorageReference imageReference, string path, int id)
    {
        Task imageTask = imageReference.GetFileAsync
        (
            path,
            new StorageProgress<DownloadState>((DownloadState state) =>
            {
                DebugLog(string.Format(
                    "Progress: {0} of {1} bytes transferred.",
                    state.BytesTransferred,
                    state.TotalByteCount
                ));

                if (state.BytesTransferred > 0 && state.TotalByteCount > 0)
                {
                    Courts.Instance.UpdateCourtImageDownloadProgress(id, (int)((float)state.BytesTransferred / state.TotalByteCount * 100));
                }
            }),
            CancellationToken.None
        );

        await imageTask.ContinueWith(resultTask =>
        {
            if (!resultTask.IsFaulted && !resultTask.IsCanceled)
            {
                Courts.Instance.SetCourtImage(id, path);
            }
        });
    }

    public async Task SaveProfile()
    {
        m_SaveProfileLoadingBar.SetActive(true);

        string userId = PlayerPrefs.GetString("UserID", string.Empty);
        if (userId == string.Empty)
        {
            m_SaveProfileLoadingBar.SetActive(false);
            return;
        }

        await UpdateDisplayName(ProfileTab.Instance.m_NameInputField.text);

        DocumentReference documentReference = firestore.Collection("Profiles").Document(userId);
        Dictionary<string, object> profile = new Dictionary<string, object>
        {
            ["UserID"] = userId,
            ["Name"] = ProfileTab.Instance.m_NameInputField.text,
            ["TeamPosition"] = ProfileTab.Instance.m_CardPosition.text,
            ["CardTopColor"] = ColorUtility.ToHtmlStringRGB(ProfileTab.Instance.m_CardTopColor.color),
            ["CardBottomColor"] = ColorUtility.ToHtmlStringRGB(ProfileTab.Instance.m_CardBottomColor.color),
        };
        
        await documentReference.SetAsync(profile).ContinueWithOnMainThread(task =>
        {
            DebugLog("Save Profile Success");
        });

        m_SaveProfileLoadingBar.SetActive(false);
    }

    public async Task GetProfile()
    {
        m_SaveProfileLoadingBar.SetActive(true);

        string userId = PlayerPrefs.GetString("UserID", string.Empty);
        if (userId == string.Empty)
        {
            m_SaveProfileLoadingBar.SetActive(false);
            return;
        }

        DocumentReference documentReference = firestore.Collection("Profiles").Document(userId);
        
        await documentReference.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                DebugLog("documentReference.GetSnapshotAsync() was canceled.");
                m_SaveProfileLoadingBar.SetActive(false);
                return;
            }
            if (task.IsFaulted)
            {
                DebugLog("documentReference.GetSnapshotAsync() encountered an error: " + task.Exception);
                m_SaveProfileLoadingBar.SetActive(false);
                return;
            }

            DocumentSnapshot documentSnapshot = task.Result;

            if (documentSnapshot.Exists)
            {
                Dictionary<string, object> profile = documentSnapshot.ToDictionary();

                string cardName = profile["Name"].ToString();
                string teamPosition = profile["TeamPosition"].ToString();
                string cardTopColor = profile["CardTopColor"].ToString();
                string cardBottomColor = profile["CardBottomColor"].ToString();
                _ = ProfileTab.Instance.m_CardTopColor.color;
                ColorUtility.TryParseHtmlString("#" + cardTopColor, out Color topColor);
                ProfileTab.Instance.m_CardTopColor.color = topColor;
                ProfileTab.Instance.m_TopColor.color = topColor;
                _ = ProfileTab.Instance.m_CardBottomColor.color;
                ColorUtility.TryParseHtmlString("#" + cardBottomColor, out Color bottomColor);
                ProfileTab.Instance.m_CardBottomColor.color = bottomColor;
                ProfileTab.Instance.m_BottomColor.color = bottomColor;
                int index = ProfileTab.Instance.m_TeamPositionSelector.elements.IndexOf(teamPosition);
                ProfileTab.Instance.m_TeamPositionSelector.index = index;
                ProfileTab.Instance.m_TeamPositionSelector.defaultIndex = index;
                ProfileTab.Instance.m_TeamPositionSelector.SetValueAtIndex();
                ProfileTab.Instance.m_CardName.text = cardName;
                ProfileTab.Instance.m_CardName.color = ProfileTab.Instance.SetInvertedColor(ProfileTab.Instance.m_CardTopColor.color);
                ProfileTab.Instance.m_CardPosition.text = ProfileTab.Instance.m_TeamPositionSelector.elements[index];
                ProfileTab.Instance.m_CardPosition.color = ProfileTab.Instance.SetInvertedColor(ProfileTab.Instance.m_CardBottomColor.color);
            }
            else
            {
                DebugLog(string.Format("Document {0} does not exist!", documentSnapshot.Id));
            }

            m_SaveProfileLoadingBar.SetActive(false);
        });
    }

    private void OnApplicationQuit()
    {
        if (firestore != null)
        {
            firestore.TerminateAsync();
            firestore.ClearPersistenceAsync();
        }
    }

    private void DebugLog(string text)
    {
        Debug.Log(text);
        debugText.text += text + "\n";
    }
}
