using Michsky.UI.Frost;
using UnityEngine;

public class Main : MonoBehaviour
{
    public static Main Instance;

    [SerializeField] private GameObject m_LoadingBar;
    [SerializeField] private GameObject m_SplashScreen;
    [SerializeField] private GameObject m_Background;
    [SerializeField] private GameObject m_MainPanel;
    [SerializeField] public GameObject m_MenuManager;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ShowLoginLoadingBar();
    }

    public void ShowLoginLoadingBar()
    {
        m_SplashScreen.GetComponent<Animator>().Play("Login to Loading");
    }

    public void HideLoginLoadingBar()
    {
        m_MenuManager.GetComponent<BlurManager>().BlurOutAnim();
        m_LoadingBar.GetComponent<CanvasGroup>().alpha = 0;
    }

    public void SwitchToMainPanel()
    {
        m_MenuManager.GetComponent<BlurManager>().BlurOutAnim();
        m_SplashScreen.GetComponent<Animator>().Play("Panel Out");
        m_MainPanel.GetComponent<Animator>().Play("Panel Start");
        m_Background.GetComponent<Animator>().Play("Switch");
        m_LoadingBar.GetComponent<CanvasGroup>().alpha = 0;
    }

    public void SwitchToLoginPanel()
    {
        AppData._UserID = "";
        AppData._UserName = "";
        AppData._UserEmail = "";
        AppData._SocialPlatform = "";
        AppData._IsLoggedIn = false;
        m_MenuManager.GetComponent<TopPanelManager>().PanelAnim(0);
        m_MenuManager.GetComponent<BlurManager>().BlurInAnim();
        m_SplashScreen.GetComponent<Animator>().Play("Login");
        m_MainPanel.GetComponent<Animator>().Play("Wait");
        m_Background.GetComponent<Animator>().Play("Start");
        m_LoadingBar.GetComponent<CanvasGroup>().alpha = 0;
    }

    public void GoogleSignIn()
    {
        FirebaseManager.Instance.OnGoogleSignIn();
    }

    public void FacebookSignIn()
    {
        FirebaseManager.Instance.OnFacebookSignIn();
    }
}
