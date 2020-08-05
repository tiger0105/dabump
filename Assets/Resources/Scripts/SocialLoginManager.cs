using Facebook.Unity;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Google;
using Michsky.UI.Frost;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class SocialLoginManager : MonoBehaviour
{
    private readonly string webClientId = "1013499565612-oto0j58l915sbmcplota9a3evbpugev2.apps.googleusercontent.com";
    private GoogleSignInConfiguration configuration;
    private Firebase.DependencyStatus dependencyStatus = Firebase.DependencyStatus.UnavailableOther;
    private FirebaseAuth auth;
    private FirebaseUser user;

    [SerializeField] private GameObject m_LoadingBar;
    [SerializeField] private GameObject m_SplashScreen;
    [SerializeField] private GameObject m_Background;
    [SerializeField] private GameObject m_MainPanel;
    [SerializeField] private GameObject m_MenuManager;

    public TextMeshProUGUI debugText;

    private void Awake()
    {
        configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestIdToken = true
        };
    }

    private void Start()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                DebugConsole("Firebase initializing...");
                InitializeFirebase();
            }
            else
            {
                DebugConsole("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
    }

    private void InitializeFirebase()
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
    }

    private void AuthStateChanged(object sender, EventArgs eventArgs)
    {
    }

    private void OnDestroy()
    {
        if (auth != null)
            auth.StateChanged -= AuthStateChanged;
        auth = null;
    }

    #region Google SignIn
    public void GoogleSignIn_Clicked()
    {
        OnGoogleSignIn();
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
            using (IEnumerator<Exception> enumerator = task.Exception.InnerExceptions.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    GoogleSignIn.SignInException error = (GoogleSignIn.SignInException)enumerator.Current;
                    HideLoginLoadingBar();
                    DebugConsole("OnGoogleAuthenticationFinished " + error.ToString());
                }
                else
                {
                    HideLoginLoadingBar();
                    DebugConsole("OnGoogleAuthenticationFinished enumerator.MoveNext() task.IsFaulted");
                }
            }
        }
        else if (task.IsCanceled)
        {
            HideLoginLoadingBar();
            DebugConsole("OnGoogleAuthenticationFinished task.IsCanceled");
        }
        else
        {
            Credential credential = GoogleAuthProvider.GetCredential(task.Result.IdToken, null);
            auth.SignInWithCredentialAsync(credential).ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    HideLoginLoadingBar();
                    DebugConsole("OnGoogleAuthenticationFinished t.IsCanceled");
                    return;
                }

                if (t.IsFaulted)
                {
                    HideLoginLoadingBar();
                    DebugConsole("OnGoogleAuthenticationFinished t.IsFaulted");
                    return;
                }

                user = auth.CurrentUser;

                PlayerPrefs.SetString("UserID", user.UserId);
                PlayerPrefs.SetString("UserName", user.DisplayName);
                PlayerPrefs.SetString("UserEmail", user.Email);
                PlayerPrefs.SetString("SocialPlatform", "Google");
                PlayerPrefs.SetInt("IsLoggedIn", 1);

                ProfileTab.Instance.LoadProfileTab();
                AccountTab.Instance.LoadAccountTab();

                SwitchToMainPanel();
                return;
            });
        }

        HideLoginLoadingBar();
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
            DebugConsole("FB Logged In.");
            DebugConsole("Start Firebase Auth");
            DebugConsole("IdToken: " + AccessToken.CurrentAccessToken.TokenString);

            FacebookAuth(AccessToken.CurrentAccessToken.TokenString);
        }
        else
        {
            HideLoginLoadingBar();
            DebugConsole("User cancelled login");
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
                DebugConsole("SignInWithCredentialAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                HideLoginLoadingBar();
                DebugConsole("SignInWithCredentialAsync encountered an error: " + task.Exception);
                return;
            }

            user = auth.CurrentUser;

            PlayerPrefs.SetString("UserID", user.UserId);
            PlayerPrefs.SetString("UserName", user.DisplayName);
            PlayerPrefs.SetString("UserEmail", user.Email);
            PlayerPrefs.SetString("SocialPlatform", "Facebook");
            PlayerPrefs.SetInt("IsLoggedIn", 1);

            ProfileTab.Instance.LoadProfileTab();
            AccountTab.Instance.LoadAccountTab();

            SwitchToMainPanel();
            return;
        });

        HideLoginLoadingBar();
    }

    private void InitCallback()
    {
        DebugConsole("FB Init done.");

        if (FB.IsLoggedIn)
        {
            DebugConsole(string.Format("FB Logged In. TokenString:" + AccessToken.CurrentAccessToken.TokenString));
            DebugConsole(AccessToken.CurrentAccessToken.ToString());

            if (PlayerPrefs.GetInt("IsLoggedIn", 0) == 1
                && PlayerPrefs.GetString("SocialPlatform", "Google") == "Facebook"
                && PlayerPrefs.GetString("UserID", string.Empty) != string.Empty)
            {
                FacebookAuth(AccessToken.CurrentAccessToken.TokenString);
            }
            else
            {
                DebugConsole("User not yet loged FB or loged out");
            }
        }
        else
        {
            DebugConsole("User cancelled login");
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

    private void SetProfileInfo()
    {
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference("CoinPurchase");

        reference.RunTransaction(SetPurchaseStateTransaction)
          .ContinueWithOnMainThread(task => {
              if (task.Exception != null)
              {
              }
              else if (task.IsCompleted)
              {
              }

              HideLoginLoadingBar();
          });
    }

    private TransactionResult SetPurchaseStateTransaction(MutableData mutableData)
    {
        if (!(mutableData.Value is List<object> userData))
        {
            userData = new List<object>();
        }

        string userId = PlayerPrefs.GetString("UserID", string.Empty);
        if (userId == string.Empty)
        {
            return TransactionResult.Abort();
        }

        bool userExists = false;

        for (int i = 0; i < userData.Count; i++)
        {
            string uId = (string)((Dictionary<string, object>)userData[i])["UserID"];
            if (uId == userId)
            {
                userExists = true;

                Dictionary<string, object> newItem = new Dictionary<string, object>
                {
                    ["UserID"] = userId,
                    ["IsPurchased"] = (PlayerPrefs.GetInt("IsPurchased", 0) == 0) ? false : true
                };

                userData[i] = newItem;
            }
        }

        if (!userExists)
        {
            Dictionary<string, object> newItem = new Dictionary<string, object>
            {
                ["UserID"] = userId,
                ["IsPurchased"] = (PlayerPrefs.GetInt("IsPurchased", 0) == 0) ? false : true
            };

            userData.Add(newItem);
        }

        mutableData.Value = userData;
        return TransactionResult.Success(mutableData);
    }

    private void GetProfileInfo()
    {
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference("CoinPurchase");

        reference.RunTransaction(GetPurchaseStateTransaction)
          .ContinueWithOnMainThread(task => {
              if (task.Exception != null)
              {
              }
              else if (task.IsCompleted)
              {
              }

              HideLoginLoadingBar();
          });
    }

    private TransactionResult GetPurchaseStateTransaction(MutableData mutableData)
    {
        if (!(mutableData.Value is List<object> userData))
        {
            userData = new List<object>();
        }

        string userId = PlayerPrefs.GetString("UserID", string.Empty);
        if (userId == string.Empty)
        {
            return TransactionResult.Abort();
        }

        bool userExists = false;

        for (int i = 0; i < userData.Count; i++)
        {
            string uId = (string)((Dictionary<string, object>)userData[i])["UserID"];
            if (uId == userId)
            {
                userExists = true;

                bool isPurchased = (bool)((Dictionary<string, object>)userData[i])["IsPurchased"];

                PlayerPrefs.SetInt("IsPurchased", isPurchased == false ? 0 : 1);

                if (isPurchased == true)
                {
                    //ShowPanel(3);
                }
            }
        }

        if (!userExists)
        {
            Dictionary<string, object> newItem = new Dictionary<string, object>
            {
                ["UserID"] = userId,
                ["IsPurchased"] = false
            };

            userData.Add(newItem);
            mutableData.Value = userData;
        }

        return TransactionResult.Success(mutableData);
    }

    private void DebugConsole(string text)
    {
        debugText.text += text + "\n";
    }
}
