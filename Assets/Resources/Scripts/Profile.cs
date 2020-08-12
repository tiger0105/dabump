using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using ImageAndVideoPicker;
using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Profile : MonoBehaviour
{
    public static Profile Instance;

    [Header("Profile Tab")]
    [SerializeField] public TMP_InputField m_NameInputField;
    [SerializeField] public HorizontalSelector m_TeamPositionSelector;
    [SerializeField] public Image m_TopColor;
    [SerializeField] public Image m_BottomColor;

    [SerializeField] public TextMeshProUGUI m_CardName;
    [SerializeField] public TextMeshProUGUI m_CardRank;
    [SerializeField] public TextMeshProUGUI m_CardPosition;
    [SerializeField] public Image m_CardTopColor;
    [SerializeField] public Image m_CardBottomColor;
    [SerializeField] public Button m_UploadCardPhotoButton;
    [SerializeField] public RawImage m_CardPhoto;

    private string m_TemporaryPhotoPath;

    [SerializeField] public Button m_SaveButton;
    [SerializeField] private GameObject m_LoadingBar;

    [Header("Account Tab")]
    [SerializeField] private TextMeshProUGUI m_Connected;
    [SerializeField] private Image m_SocialIcon;
    [SerializeField] private TextMeshProUGUI m_SocialLoginEmail;
    [SerializeField] private Button m_DeleteAccountButton;

    [SerializeField] private Sprite[] m_SocialIcons;

    [Header("Help Tab")]
    [SerializeField] private TMP_InputField m_HelpForm_NameInputField;
    [SerializeField] private TMP_InputField m_HelpForm_EmailInputField;
    [SerializeField] private TMP_InputField m_HelpForm_QuestionInputField;

    [SerializeField] private TextMeshProUGUI m_HelpForm_StatusText;
    [SerializeField] private Button m_HelpForm_SubmitButton;
    [SerializeField] private GameObject m_HelpForm_LoadingBar;

    [Header("Right Panel")]
    [SerializeField] private RawImage m_RightPanel_Photo;
    [SerializeField] private TextMeshProUGUI m_RightPanel_Name;
    [SerializeField] private TextMeshProUGUI m_RightPanel_TeamPosition;
    [SerializeField] private TextMeshProUGUI m_RightPanel_Badge;
    [SerializeField] private TextMeshProUGUI m_RightPanel_Rank;
    [SerializeField] private GameObject m_RightPanel_IsMVP;


    private void Awake()
    {
        Instance = this;
    }

    public async Task DeleteAccountAsync(FirebaseUser user, FirebaseAuth auth, FirebaseFirestore firestore)
    {
        if (user != null)
        {
            string userId = PlayerPrefs.GetString("UserID", string.Empty);
            if (userId == string.Empty)
            {
                return;
            }

            DocumentReference documentReference = firestore.Collection("Profiles").Document(userId);

            await documentReference.DeleteAsync().ContinueWithOnMainThread(async t =>
            {
                if (t.IsCompleted)
                {
                    await user.DeleteAsync().ContinueWith(task =>
                    {
                        if (task.IsCanceled)
                        {
                            Debug.Log("DeleteAsync was canceled.");
                            return;
                        }

                        if (task.IsFaulted)
                        {
                            Debug.Log("DeleteAsync encountered an error: " + task.Exception);
                            return;
                        }

                        if (auth != null)
                        {
                            auth.SignOut();
                            Main.Instance.SwitchToLoginPanel();
                        }
                    });
                }
            });
        }
    }

    public async Task UpdateDisplayName(FirebaseUser user, string name)
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

    public void SaveProfile()
    {
        FirebaseManager.Instance.SaveProfile();
    }

    public void DeleteAccount()
    {
        FirebaseManager.Instance.DeleteAccount();
    }

    public async Task SaveProfileAsync(FirebaseUser user, FirebaseFirestore firestore)
    {
        m_LoadingBar.SetActive(true);

        string userId = PlayerPrefs.GetString("UserID", string.Empty);
        if (userId == string.Empty)
        {
            m_LoadingBar.SetActive(false);
            return;
        }

        await UpdateDisplayName(user, m_NameInputField.text);

        bool isImageUploaded = false;

        if (m_TemporaryPhotoPath.Length > 0 && File.Exists(m_TemporaryPhotoPath))
        {
            await FirebaseManager.Instance.UploadProfilePhotoAsync(m_TemporaryPhotoPath, "Profiles/" + userId + ".jpg");
            isImageUploaded = true;
        }

        DocumentReference documentReference = firestore.Collection("Profiles").Document(userId);
        Dictionary<string, object> profile = new Dictionary<string, object>
        {
            ["UserID"] = userId,
            ["Name"] = m_NameInputField.text,
            ["TeamPosition"] = m_CardPosition.text,
            ["CardTopColor"] = ColorUtility.ToHtmlStringRGB(m_CardTopColor.color),
            ["CardBottomColor"] = ColorUtility.ToHtmlStringRGB(m_CardBottomColor.color),
        };

        if (isImageUploaded == true)
            profile.Add("Image", "Profiles/" + userId + ".jpg");

        await documentReference.SetAsync(profile).ContinueWithOnMainThread(task =>
        {

        });

        m_LoadingBar.SetActive(false);
    }

    public IEnumerator GetProfileAsync()
    {
        m_LoadingBar.SetActive(true);

        //string userId = PlayerPrefs.GetString("UserID", "4lksKO4kUHZc9fXNpmEiz5N5iro1");
        string userId = PlayerPrefs.GetString("UserID", string.Empty);
        string userName = PlayerPrefs.GetString("UserName", string.Empty);
        if (userId == string.Empty)
        {
            m_LoadingBar.SetActive(false);
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("userId", userId);
        form.AddField("userName", userName);

        using (UnityWebRequest www = UnityWebRequest.Post(AppData._REST_API_ENDPOINT + AppData._REST_API_GET_MY_PROFILE, form))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                m_LoadingBar.SetActive(false);
                yield break;
            }

            Debug.Log(www.downloadHandler.text);

            PlayerProfile playerProfile = JsonUtility.FromJson<PlayerProfile>(www.downloadHandler.text);

            ColorUtility.TryParseHtmlString("#" + playerProfile.CardTopColor, out Color topColor);
            m_CardTopColor.color = topColor;
            m_TopColor.color = topColor;
            ColorUtility.TryParseHtmlString("#" + playerProfile.CardBottomColor, out Color bottomColor);
            m_CardBottomColor.color = bottomColor;
            m_BottomColor.color = bottomColor;
            int index = m_TeamPositionSelector.elements.IndexOf(playerProfile.TeamPosition.ToUpper());
            m_TeamPositionSelector.index = index;
            m_TeamPositionSelector.defaultIndex = index;
            m_TeamPositionSelector.SetValueAtIndex();
            m_NameInputField.text = playerProfile.Name;
            m_CardName.text = playerProfile.Name;
            m_CardName.color = SetInvertedColor(m_CardTopColor.color);
            m_CardRank.text = playerProfile.Rank == 0 ? "Rank NA" : ("Rank " + playerProfile.Rank);
            m_CardPosition.text = m_TeamPositionSelector.elements[index];
            m_CardPosition.color = SetInvertedColor(m_CardBottomColor.color);

            //m_CardPhoto 

            //m_RightPanel_Photo

            m_RightPanel_Name.text = playerProfile.Name;
            m_RightPanel_TeamPosition.text = playerProfile.TeamPosition.ToUpper();
            m_RightPanel_Rank.text = playerProfile.Rank == 0 ? "Rank Not Available" : ("Rank " + playerProfile.Rank);
            m_RightPanel_Badge.text = playerProfile.Badges == 0 ? "Not Available" : playerProfile.Badges.ToString();
            m_RightPanel_IsMVP.SetActive(playerProfile.IsMVP);
        }

        m_Connected.text = PlayerPrefs.GetInt("IsLoggedIn", 0) == 0 ? string.Empty : "Connected";
        m_SocialIcon.sprite = PlayerPrefs.GetString("SocialPlatform", "Google") == "Google" ? m_SocialIcons[0] : m_SocialIcons[1];
        m_SocialLoginEmail.text = PlayerPrefs.GetString("UserEmail", string.Empty);
        if (m_SocialLoginEmail.text == string.Empty)
            m_SocialLoginEmail.text = "Email is not set as public. Please sign in again.";

        m_LoadingBar.SetActive(false);

        PlayerInfo.Instance.BuildPlayerInfoList();
        Main.Instance.SwitchToMainPanel();
    }

    public Color SetInvertedColor(Color original)
    {
        Color inverted = new Color(1 - original.r, 1 - original.g, 1 - original.b);
        if (inverted.r >= 0.45f && inverted.r <= 0.55f
            && inverted.g >= 0.45f && inverted.g <= 0.55f
            && inverted.b >= 0.45f && inverted.b <= 0.55f)
        {
            inverted = Color.black;
        }

        return inverted;
    }

    public void OnNameInputFieldChanged()
    {
        m_CardName.text = m_NameInputField.text;
    }

    public void OnTeamPositionChanged()
    {
        m_CardPosition.text = m_TeamPositionSelector.elements[m_TeamPositionSelector.index];
    }

    public void OnCardTopColorUpdated()
    {
        m_CardName.color = SetInvertedColor(m_CardTopColor.color);
    }

    public void OnCardBottomColorUpdated()
    {
        m_CardPosition.color = SetInvertedColor(m_CardBottomColor.color);
    }

    public void SelectAvatarPhoto()
    {
#if UNITY_ANDROID
        AndroidPicker.BrowseImage(true);
#elif UNITY_IPHONE
		IOSPicker.BrowseImage(true);
#endif
    }

    private void OnEnable()
    {
        PickerEventListener.onImageSelect += OnImageSelect;
        PickerEventListener.onImageLoad += OnImageLoad;
        PickerEventListener.onError += OnError;
        PickerEventListener.onCancel += OnCancel;

#if UNITY_ANDROID
        AndroidPicker.CheckPermissions();
#endif
    }

    private void OnDisable()
    {
        PickerEventListener.onImageSelect -= OnImageSelect;
        PickerEventListener.onImageLoad -= OnImageLoad;
        PickerEventListener.onError -= OnError;
        PickerEventListener.onCancel -= OnCancel;
    }

    private void OnImageSelect(string imgPath, ImageOrientation imgOrientation)
    {
        Debug.Log("Image Location : " + imgPath);
    }

    private void OnImageLoad(string imgPath, Texture2D tex, ImageOrientation imgOrientation)
    {
        Debug.Log("Image Location : " + imgPath);
        m_TemporaryPhotoPath = imgPath;
        m_CardPhoto.texture = tex;
        m_UploadCardPhotoButton.transform.GetChild(0).gameObject.SetActive(false);
    }

    private void OnError(string errorMsg)
    {
        Debug.Log("Error : " + errorMsg);
        m_TemporaryPhotoPath = "";
    }

    private void OnCancel()
    {
        Debug.Log("Cancel by user");
        m_TemporaryPhotoPath = ""; 
    }

    public void SubmitHelpForm()
    {
        StartCoroutine(SubmitHelpFormAsync());
    }

    private IEnumerator SubmitHelpFormAsync()
    {
        m_HelpForm_LoadingBar.SetActive(true);
        m_HelpForm_SubmitButton.enabled = false;

        m_HelpForm_StatusText.text = "Submitting...";

        WWWForm form = new WWWForm();
        form.AddField("name", m_HelpForm_NameInputField.text);
        form.AddField("email", m_HelpForm_EmailInputField.text);
        form.AddField("question", m_HelpForm_QuestionInputField.text);

        using (UnityWebRequest www = UnityWebRequest.Post(AppData._REST_API_ENDPOINT + AppData._REST_API_SUBMIT_HELP_FORM, form))
        {
            yield return www.SendWebRequest();

            if (www.responseCode != 200 || www.downloadHandler.text != "200")
            {
                m_HelpForm_StatusText.text = "Failed to submit the help form. Please try again later.";
                m_HelpForm_LoadingBar.SetActive(false);
                m_HelpForm_SubmitButton.enabled = true;
                yield break;
            }

            m_HelpForm_StatusText.text = "Help form submitted successfully.";
        }

        m_HelpForm_LoadingBar.SetActive(false);
        m_HelpForm_SubmitButton.enabled = true;
    }
}
