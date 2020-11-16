using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using ImageAndVideoPicker;
using Michsky.UI.Frost;
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

    [SerializeField] private Transform m_CourtListTransform;

    [SerializeField] private ToggleGroup m_Tab;

    [Header("Profile Tab")]
    [SerializeField] private Animator m_ProfileTabAnimator;
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

    private string m_TemporaryPhotoPath = "";

    [SerializeField] public Button m_SaveButton;
    [SerializeField] public GameObject m_LoadingBar;

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
    [SerializeField] public TextMeshProUGUI m_RightPanel_Name;
    [SerializeField] public TextMeshProUGUI m_RightPanel_TeamPosition;
    [SerializeField] public TextMeshProUGUI m_RightPanel_Badge;
    [SerializeField] public TextMeshProUGUI m_RightPanel_Rank;
    [SerializeField] private GameObject m_RightPanel_IsMVP;


    private void Awake()
    {
        Instance = this;
    }

    public void TabButtonClicked(bool isOn)
    {
        if (isOn == true)
        {
            Toggle activeToggle = m_Tab.ActiveToggles().FirstOrDefault();
            if (activeToggle != null)
            {
                GetComponent<TopPanelManager>().PanelAnim(activeToggle.transform.GetSiblingIndex());
            }
        }
    }

    public void ActivateProfileTab()
    {
        m_Tab.transform.GetChild(0).GetComponent<Toggle>().isOn = true;
        m_ProfileTabAnimator.Play("Panel In");
    }

    private void Start()
    {
        PickerEventListener.onImageLoad += OnImageLoad;
        PickerEventListener.onError += OnError;
        PickerEventListener.onCancel += OnCancel;

#if UNITY_ANDROID
        AndroidPicker.CheckPermissions();
#endif
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

            await FirebaseManager.Instance.DeleteProfilePhotoAsync("Profiles/" + userId + ".jpg");

            await documentReference.DeleteAsync().ContinueWithOnMainThread(async t =>
            {
                if (t.IsCompleted)
                {
                    await user.DeleteAsync().ContinueWith(task =>
                    {
                        if (task.IsCanceled)
                        {
                            FirebaseManager.Instance.DebugLog("DeleteAsync was canceled.");
                            return;
                        }

                        if (task.IsFaulted)
                        {
                            FirebaseManager.Instance.DebugLog("DeleteAsync encountered an error: " + task.Exception);
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

    public void SaveProfile()
    {
        FirebaseManager.Instance.SaveProfile(m_TemporaryPhotoPath);
    }

    public void SetProfileImage(string imagePath)
    {
#if UNITY_ANDROID
        string filePath = Application.persistentDataPath + "/" + imagePath;
#elif UNITY_IOS
        string filePath = Application.persistentDataPath + "/" + imagePath;
#endif
        FileInfo fileInfo = new FileInfo(filePath);

        if (fileInfo == null || !fileInfo.Exists) return;

        MemoryStream dest = new MemoryStream();

        using (Stream source = fileInfo.OpenRead())
        {
            byte[] buffer = new byte[2048];
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                dest.Write(buffer, 0, bytesRead);
            }
        }

        FirebaseManager.Instance.DebugLog("Profile Image Assigned");

        byte[] imageBytes = dest.ToArray();

        Texture2D texture = new Texture2D(296, 370);
        texture.LoadImage(imageBytes);

        FirebaseManager.Instance.DebugLog("Profile Image Loaded");

        float aspectRatio = (float)texture.width / texture.height;

        m_CardPhoto.texture = texture;
        m_CardPhoto.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        m_CardPhoto.GetComponent<AspectRatioFitter>().aspectRatio = aspectRatio;

        m_RightPanel_Photo.texture = texture;
        m_RightPanel_Photo.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        m_RightPanel_Photo.GetComponent<AspectRatioFitter>().aspectRatio = aspectRatio;
    }

    public void DeleteAccount()
    {
        FirebaseManager.Instance.DeleteAccount();
    }

    public IEnumerator GetProfileAsync()
    {
        m_LoadingBar.SetActive(true);
#if UNITY_EDITOR
        string userId = PlayerPrefs.GetString("UserID", "X6iO1w2GebNDkf4nQsWLYqovIQh2");
        string userName = PlayerPrefs.GetString("UserName", "Sweetmei Sprietsma");
#else
        string userId = PlayerPrefs.GetString("UserID", string.Empty);
        string userName = PlayerPrefs.GetString("UserName", string.Empty);
#endif
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

            FirebaseManager.Instance.DebugLog(www.downloadHandler.text);

            PlayerProfile playerProfile = JsonUtility.FromJson<PlayerProfile>(www.downloadHandler.text);


            AppData._PlayerProfile = playerProfile;

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
            m_CardPosition.text = m_TeamPositionSelector.elements[index];
            m_CardPosition.color = SetInvertedColor(m_CardBottomColor.color);

            FirebaseManager.Instance.DebugLog("Player Profile From Json Worked");

            int rank = 0;
            FirebaseManager.Instance.DebugLog(playerProfile.Image);
            if (FirebaseManager.Instance.m_CourtList.Count > 0)
            {
                FirebaseManager.Instance.DebugLog("before setting up a rank");
                if (FirebaseManager.Instance.m_PlayerCardList.Count > 0)
                {
                    PlayerProfile p = FirebaseManager.Instance.m_PlayerCardList.FirstOrDefault(item => item.UserID == userId);
                    if (p != null)
                        rank = p.Rank;
                }
                FirebaseManager.Instance.DebugLog("after setting up a rank");
            }
            m_CardRank.text = rank == 0 ? "Rank NA" : ("Rank " + rank);
            
            if (playerProfile.Image.Length > 0)
            {
                FirebaseManager.Instance.DebugLog("if player image is valid");
                SetProfileImage(playerProfile.Image);
                m_UploadCardPhotoButton.transform.GetChild(0).gameObject.SetActive(false);
            }

            FirebaseManager.Instance.DebugLog("After Setting up Upload card photo button");

            m_RightPanel_Name.text = playerProfile.Name;
            m_RightPanel_TeamPosition.text = playerProfile.TeamPosition.ToUpper();
            m_RightPanel_Rank.text = rank == 0 ? "Rank Not Available" : ("Rank " + rank);
            m_RightPanel_Badge.text = playerProfile.Badges == 0 ? "Not Available" : playerProfile.Badges.ToString();
            m_RightPanel_IsMVP.SetActive(playerProfile.IsMVP);

            FirebaseManager.Instance.DebugLog("Before SetCourtCheckedInAndBadgeStatus");

            SetCourtCheckedInAndBadgeStatus(playerProfile);

            FirebaseManager.Instance.DebugLog("After SetCourtCheckedInAndBadgeStatus");
        }

        m_Connected.text = PlayerPrefs.GetInt("IsLoggedIn", 0) == 0 ? string.Empty : "Connected";
        m_SocialIcon.sprite = PlayerPrefs.GetString("SocialPlatform", "Google") == "Google" ? m_SocialIcons[0] :
            (PlayerPrefs.GetString("SocialPlatform", "Google") == "Facebook" ? m_SocialIcons[1] : m_SocialIcons[2]);
        m_SocialLoginEmail.text = PlayerPrefs.GetString("UserEmail", string.Empty);
        if (m_SocialLoginEmail.text == string.Empty)
            m_SocialLoginEmail.text = "Email is not set as public. Please sign in again.";

        m_LoadingBar.SetActive(false);

        PlayerInfo.Instance.BuildPlayerInfoList();
        Main.Instance.SwitchToMainPanel();
    }

    public void SetCourtCheckedInAndBadgeStatus(PlayerProfile profile)
    {
        List<int> visitedCourts = new List<int>();
        if (profile.VisitedCourts.Length > 0)
        {
            visitedCourts = profile.VisitedCourts.Split(',').Select(int.Parse).ToList();
        }

        for (int i = 0; i < visitedCourts.Count; i ++)
        {
            if (visitedCourts[i] > m_CourtListTransform.childCount)
                continue;

            m_CourtListTransform.GetChild(visitedCourts[i] - 1).GetChild(1).GetChild(5).GetComponent<Image>().color = Color.white;
        }

        if (profile.CheckedInCourt > 0 && profile.CheckedInCourt <= m_CourtListTransform.childCount)
        {
            m_CourtListTransform.GetChild(profile.CheckedInCourt - 1).GetChild(1).GetChild(4).gameObject.SetActive(true);
        }
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

    private void OnImageLoad(string imgPath, Texture2D tex, ImageOrientation imgOrientation)
    {
        m_TemporaryPhotoPath = imgPath;
        m_CardPhoto.texture = tex;
        m_RightPanel_Photo.texture = tex;
        m_RightPanel_Photo.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        m_RightPanel_Photo.GetComponent<AspectRatioFitter>().aspectRatio = (float)tex.width / tex.height;

        m_UploadCardPhotoButton.transform.GetChild(0).gameObject.SetActive(false);
    }

    private void OnError(string errorMsg)
    {
        m_TemporaryPhotoPath = "";
    }

    private void OnCancel()
    {
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
