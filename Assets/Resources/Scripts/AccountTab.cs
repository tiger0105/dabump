using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AccountTab : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_Connected;
    [SerializeField] private Image m_SocialIcon;
    [SerializeField] private TextMeshProUGUI m_SocialLoginEmail;
    [SerializeField] private Button m_PasswordButton;
    [SerializeField] private Button m_DeleteAccountButton;

    [SerializeField] private Sprite[] m_SocialIcons;

    public static AccountTab Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadAccountTab();
    }

    public void LoadAccountTab()
    {
        m_Connected.text = PlayerPrefs.GetInt("IsLoggedIn", 0) == 0 ? string.Empty : "Connected";
        m_SocialIcon.sprite = PlayerPrefs.GetString("SocialPlatform", "Google") == "Google" ? m_SocialIcons[0] : m_SocialIcons[1];
        m_SocialLoginEmail.text = PlayerPrefs.GetString("UserEmail", string.Empty);
        if (m_SocialLoginEmail.text == string.Empty)
            m_SocialLoginEmail.text = "Email is not set as public for this app.";
    }

    public void OnChangePasswordButtonClicked()
    {
        
    }

    public void OnDeleteAccountButtonClicked()
    {

    }
}
