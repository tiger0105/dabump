using Facebook.Unity;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Google;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SocialLoginManager : MonoBehaviour
{
    private readonly string webClientId = "543189817157-l9aeb9kc7qgamf84tl23bus6rk25q7e8.apps.googleusercontent.com";
    private GoogleSignInConfiguration configuration;
    private Firebase.DependencyStatus dependencyStatus = Firebase.DependencyStatus.UnavailableOther;
    private FirebaseAuth auth;
    private FirebaseUser user;

    private bool m_IsRecovery = false;
    private bool IsAskedForLogin = false;

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
    }

    #region Google SignIn
    public void GoogleSignIn_Clicked()
    {
        OnGoogleSignIn();
    }

    public void RecoveryButton_Clicked()
    {
        m_IsRecovery = true;

        //ShowSocialLoginPanel();
    }

    private void OnGoogleSignIn()
    {
        //m_LoadingPanel.SetActive(true);

        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = false;
        GoogleSignIn.Configuration.RequestIdToken = true;

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(
          OnGoogleAuthenticationFinished);
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
                    //m_LoadingPanel.SetActive(false);
                }
                else
                {
                    //m_LoadingPanel.SetActive(false);
                }
            }
        }
        else if (task.IsCanceled)
        {
            //m_LoadingPanel.SetActive(false);
        }
        else
        {
            Credential credential = GoogleAuthProvider.GetCredential(task.Result.IdToken, null);
            auth.SignInWithCredentialAsync(credential).ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    //m_LoadingPanel.SetActive(false);
                    return;
                }

                if (t.IsFaulted)
                {
                    //m_LoadingPanel.SetActive(false);
                    return;
                }

                user = auth.CurrentUser;

                PlayerPrefs.SetString("UserID", user.UserId);
                //PlayerPrefs.SetInt("SocialPlatform", (int)SOCIAL_PLATFORM.Google);
                PlayerPrefs.SetInt("IsLoggedIn", 1);

                if (m_IsRecovery == false)
                    SetUserPurchasedState();
                else
                    GetUserPurchasedState();
            });
        }

        //CloseSocialLoginPanel();
    }
    #endregion

    #region Facebook SignIn
    public void FacebookSignIn_Click()
    {
        OnFacebookSignIn();
    }

    private void OnFacebookSignIn()
    {
        //m_LoadingPanel.SetActive(true);

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
            Debug.Log("User cancelled login");
            //m_LoadingPanel.SetActive(false);
        }

        //CloseSocialLoginPanel();
    }

    private void FacebookAuth(string accessToken)
    {
        Credential credential = FacebookAuthProvider.GetCredential(accessToken);

        auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("SignInWithCredentialAsync was canceled.");
                //m_LoadingPanel.SetActive(false);
                return;
            }
            if (task.IsFaulted)
            {
                Debug.Log("SignInWithCredentialAsync encountered an error: " + task.Exception);
                //m_LoadingPanel.SetActive(false);
                return;
            }

            user = auth.CurrentUser;

            PlayerPrefs.SetString("UserID", user.UserId);
            //PlayerPrefs.SetInt("SocialPlatform", (int)SOCIAL_PLATFORM.Facebook);
            PlayerPrefs.SetInt("IsLoggedIn", 1);

            if (m_IsRecovery == false)
                SetUserPurchasedState();
            else
                GetUserPurchasedState();
        });
    }

    private void InitCallback()
    {
        Debug.Log("FB Init done.");

        if (FB.IsLoggedIn)
        {
            Debug.Log(string.Format("FB Logged In. TokenString:" + AccessToken.CurrentAccessToken.TokenString));
            Debug.Log(AccessToken.CurrentAccessToken.ToString());

            if (PlayerPrefs.GetInt("IsLoggedIn", 0) == 1
                //&& PlayerPrefs.GetInt("SocialPlatform", -1) == (int)SOCIAL_PLATFORM.Facebook
                && PlayerPrefs.GetString("UserID", string.Empty) != string.Empty)
            {
                //m_LoadingPanel.SetActive(true);
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

    private void SetUserPurchasedState()
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

              //m_LoadingPanel.SetActive(false);
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

    private void GetUserPurchasedState()
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

              //m_LoadingPanel.SetActive(false);
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
}
