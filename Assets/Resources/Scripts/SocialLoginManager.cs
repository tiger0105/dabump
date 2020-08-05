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

    [SerializeField] private ProfileTab m_ProfileTab;
    [SerializeField] private AccountTab m_AccountTab;

    public static SocialLoginManager Instance;

    private void Awake()
    {
        Instance = this;

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
                Debug.Log("Firebase initializing...");
                InitializeFirebase();
            }
            else
            {
                Debug.Log("Could not resolve all Firebase dependencies: " + dependencyStatus);
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
        Instance = null;
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
                    Debug.Log("OnGoogleAuthenticationFinished " + error.ToString());
                }
                else
                {
                    HideLoginLoadingBar();
                    Debug.Log("OnGoogleAuthenticationFinished enumerator.MoveNext() task.IsFaulted");
                }
            }
        }
        else if (task.IsCanceled)
        {
            HideLoginLoadingBar();
            Debug.Log("OnGoogleAuthenticationFinished task.IsCanceled");
        }
        else
        {
            Credential credential = GoogleAuthProvider.GetCredential(task.Result.IdToken, null);
            auth.SignInWithCredentialAsync(credential).ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    HideLoginLoadingBar();
                    Debug.Log("OnGoogleAuthenticationFinished t.IsCanceled");
                    return;
                }

                if (t.IsFaulted)
                {
                    HideLoginLoadingBar();
                    Debug.Log("OnGoogleAuthenticationFinished t.IsFaulted");
                    return;
                }

                user = auth.CurrentUser;

                PlayerPrefs.SetString("UserID", user.UserId);
                PlayerPrefs.SetString("UserName", user.DisplayName);
                PlayerPrefs.SetString("UserEmail", user.Email);
                PlayerPrefs.SetString("SocialPlatform", "Google");
                PlayerPrefs.SetInt("IsLoggedIn", 1);

                ProfileTab.Instance.LoadProfileTab();
                ProfileTab.Instance.GetProfileInfo();
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
            Debug.Log("FB Logged In.");
            Debug.Log("Start Firebase Auth");
            Debug.Log("IdToken: " + AccessToken.CurrentAccessToken.TokenString);

            FacebookAuth(AccessToken.CurrentAccessToken.TokenString);
        }
        else
        {
            HideLoginLoadingBar();
            Debug.Log("User cancelled login");
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
                Debug.Log("SignInWithCredentialAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                HideLoginLoadingBar();
                Debug.Log("SignInWithCredentialAsync encountered an error: " + task.Exception);
                return;
            }

            user = auth.CurrentUser;

            PlayerPrefs.SetString("UserID", user.UserId);
            PlayerPrefs.SetString("UserName", user.DisplayName);
            PlayerPrefs.SetString("UserEmail", user.Email);
            PlayerPrefs.SetString("SocialPlatform", "Facebook");
            PlayerPrefs.SetInt("IsLoggedIn", 1);

            ProfileTab.Instance.LoadProfileTab();
            ProfileTab.Instance.GetProfileInfo();
            AccountTab.Instance.LoadAccountTab();

            SwitchToMainPanel();
            return;
        });

        HideLoginLoadingBar();
    }

    private void InitCallback()
    {
        Debug.Log("FB Init done.");

        if (FB.IsLoggedIn)
        {
            Debug.Log(string.Format("FB Logged In. TokenString:" + AccessToken.CurrentAccessToken.TokenString));
            Debug.Log(AccessToken.CurrentAccessToken.ToString());

            if (PlayerPrefs.GetInt("IsLoggedIn", 0) == 1
                && PlayerPrefs.GetString("SocialPlatform", "Google") == "Facebook"
                && PlayerPrefs.GetString("UserID", string.Empty) != string.Empty)
            {
                FacebookAuth(AccessToken.CurrentAccessToken.TokenString);
            }
            else
            {
                Debug.Log("User not yet loged FB or loged out");
            }
        }
        else
        {
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

    public void UpdateDisplayName(string name)
    {
        if (user != null)
        {
            UserProfile userProfile = new UserProfile
            {
                DisplayName = name
            };
            user.UpdateUserProfileAsync(userProfile).ContinueWith(t =>
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

                //user = auth.CurrentUser;

                //PlayerPrefs.SetString("UserID", user.UserId);
                //PlayerPrefs.SetString("UserName", user.DisplayName);
                //PlayerPrefs.SetString("UserEmail", user.Email);
                //PlayerPrefs.SetString("SocialPlatform", "Google");
                //PlayerPrefs.SetInt("IsLoggedIn", 1);

                //ProfileTab.Instance.LoadProfileTab();
                //ProfileTab.Instance.GetProfileInfo();
                //AccountTab.Instance.LoadAccountTab();

                //SwitchToMainPanel();
                //return;
            });

        }
}
