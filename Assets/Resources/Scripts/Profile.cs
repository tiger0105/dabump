using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Michsky.UI.ModernUIPack;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
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

    [SerializeField] public Button m_SaveButton;
    [SerializeField] private GameObject m_LoadingBar;

    [Header("Account Tab")]
    [SerializeField] private TextMeshProUGUI m_Connected;
    [SerializeField] private Image m_SocialIcon;
    [SerializeField] private TextMeshProUGUI m_SocialLoginEmail;
    [SerializeField] private Button m_DeleteAccountButton;

    [SerializeField] private Sprite[] m_SocialIcons;

    private void Awake()
    {
        Instance = this;
    }

    private async Task DeleteAccountAsync(FirebaseUser user, FirebaseAuth auth)
    {
        if (user != null)
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

    public async Task SaveProfile(FirebaseUser user, FirebaseFirestore firestore)
    {
        m_LoadingBar.SetActive(true);

        string userId = PlayerPrefs.GetString("UserID", string.Empty);
        if (userId == string.Empty)
        {
            m_LoadingBar.SetActive(false);
            return;
        }

        await UpdateDisplayName(user, m_NameInputField.text);

        DocumentReference documentReference = firestore.Collection("Profiles").Document(userId);
        Dictionary<string, object> profile = new Dictionary<string, object>
        {
            ["UserID"] = userId,
            ["Name"] = m_NameInputField.text,
            ["TeamPosition"] = m_CardPosition.text,
            ["CardTopColor"] = ColorUtility.ToHtmlStringRGB(m_CardTopColor.color),
            ["CardBottomColor"] = ColorUtility.ToHtmlStringRGB(m_CardBottomColor.color),
        };

        await documentReference.SetAsync(profile).ContinueWithOnMainThread(task =>
        {

        });

        m_LoadingBar.SetActive(false);
    }

    public async Task GetProfileAsync(FirebaseFirestore firestore)
    {
        m_LoadingBar.SetActive(true);

        string userId = PlayerPrefs.GetString("UserID", string.Empty);
        if (userId == string.Empty)
        {
            m_LoadingBar.SetActive(false);
            return;
        }

        DocumentReference documentReference = firestore.Collection("Profiles").Document(userId);

        await documentReference.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("documentReference.GetSnapshotAsync() was canceled.");
                m_LoadingBar.SetActive(false);
                return;
            }
            if (task.IsFaulted)
            {
                Debug.Log("documentReference.GetSnapshotAsync() encountered an error: " + task.Exception);
                m_LoadingBar.SetActive(false);
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
                _ = m_CardTopColor.color;
                ColorUtility.TryParseHtmlString("#" + cardTopColor, out Color topColor);
                m_CardTopColor.color = topColor;
                m_TopColor.color = topColor;
                _ = m_CardBottomColor.color;
                ColorUtility.TryParseHtmlString("#" + cardBottomColor, out Color bottomColor);
                m_CardBottomColor.color = bottomColor;
                m_BottomColor.color = bottomColor;
                int index = m_TeamPositionSelector.elements.IndexOf(teamPosition);
                m_TeamPositionSelector.index = index;
                m_TeamPositionSelector.defaultIndex = index;
                m_TeamPositionSelector.SetValueAtIndex();
                m_NameInputField.text = cardName;
                m_CardName.text = cardName;
                m_CardName.color = SetInvertedColor(m_CardTopColor.color);
                m_CardPosition.text = m_TeamPositionSelector.elements[index];
                m_CardPosition.color = SetInvertedColor(m_CardBottomColor.color);
            }
            else
            {
                Debug.Log(string.Format("Document {0} does not exist!", documentSnapshot.Id));
            }
        });

        m_Connected.text = PlayerPrefs.GetInt("IsLoggedIn", 0) == 0 ? string.Empty : "Connected";
        m_SocialIcon.sprite = PlayerPrefs.GetString("SocialPlatform", "Google") == "Google" ? m_SocialIcons[0] : m_SocialIcons[1];
        m_SocialLoginEmail.text = PlayerPrefs.GetString("UserEmail", string.Empty);
        if (m_SocialLoginEmail.text == string.Empty)
            m_SocialLoginEmail.text = "Email is not set as public. Please sign in again.";

        m_LoadingBar.SetActive(false);
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

    public void OnAvatarUploadButtonClicked()
    {
        if (NativeGallery.IsMediaPickerBusy())
            return;

        PickImage();
    }

    private void PickImage()
    {
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                Texture2D texture = NativeGallery.LoadImageAtPath(path);
                if (texture == null)
                {
                    return;
                }

                m_CardPhoto.texture = texture;

                Destroy(texture, 5f);
            }
        }, "Select a PNG image", "image/png");
    }
}
