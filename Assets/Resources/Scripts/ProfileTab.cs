using Firebase.Database;
using Firebase.Extensions;
using Michsky.UI.ModernUIPack;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileTab : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_NameInputField;
    [SerializeField] private HorizontalSelector m_TeamPositionSelector;
    [SerializeField] private TextMeshProUGUI m_CardName;
    [SerializeField] private TextMeshProUGUI m_CardRank;
    [SerializeField] private TextMeshProUGUI m_CardPosition;
    [SerializeField] private Image m_CardTopColor;
    [SerializeField] private Image m_CardBottomColor;
    [SerializeField] private Button m_UploadCardPhotoButton;
    [SerializeField] private RawImage m_CardPhoto;
    [SerializeField] private Button m_SaveProfileButton;
    [SerializeField] private GameObject m_SaveProfileLoadingBar;

    public static ProfileTab Instance;

    public TextMeshProUGUI debugText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadProfileTab();
    }

    private Color SetInvertedColor(Color original)
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

    public void LoadProfileTab()
    {
        m_NameInputField.text = PlayerPrefs.GetString("UserName", string.Empty);
        m_CardName.text = m_NameInputField.text;
        m_CardName.color = SetInvertedColor(m_CardTopColor.color);
        m_CardPosition.text = m_TeamPositionSelector.elements[m_TeamPositionSelector.index];
        m_CardPosition.color = SetInvertedColor(m_CardBottomColor.color);
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

    public void SetProfileInfo()
    {
        m_SaveProfileLoadingBar.SetActive(true);

        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;

        DebugLog("Database Reference: " + reference.Key);

        reference.RunTransaction(SetProfileTransaction)
          .ContinueWithOnMainThread(task => {
              if (task.IsFaulted)
              {
                  DebugLog("SetProfileInfo task.IsFaulted");
              }
              else if (task.IsCanceled)
              {
                  DebugLog("SetProfileInfo task.IsCanceled");
              }
              else if (task.IsCompleted)
              {
                  DebugLog("SetProfileInfo task.IsCompleted");
              }

              m_SaveProfileLoadingBar.SetActive(false);
          });
    }

    private TransactionResult SetProfileTransaction(MutableData mutableData)
    {
        DebugLog("SetProfileTransaction: " + JsonUtility.ToJson(mutableData.Value));
        if (!(mutableData.Value is List<object> userData))
        {
            userData = new List<object>();
        }

        string userId = PlayerPrefs.GetString("UserID", string.Empty);
        if (userId == string.Empty)
        {
            DebugLog("UserID is empty");
            return TransactionResult.Abort();
        }

        bool userExists = false;

        for (int i = 0; i < userData.Count; i++)
        {
            DebugLog((userData[i] as Dictionary<string, object>)["UserID"].ToString());
            string uId = (string)((Dictionary<string, object>)userData[i])["UserID"];
            if (uId == userId)
            {
                userExists = true;

                Dictionary<string, object> item = new Dictionary<string, object>
                {
                    ["UserID"] = userId,
                    ["Name"] = m_NameInputField.text,
                    ["TeamPosition"] = m_CardPosition.text,
                    ["CardTopColor"] = ColorUtility.ToHtmlStringRGB(m_CardTopColor.color),
                    ["CardBottomColor"] = ColorUtility.ToHtmlStringRGB(m_CardBottomColor.color),
                };

                userData[i] = item;

                break;
            }
        }

        if (!userExists)
        {
            Dictionary<string, object> item = new Dictionary<string, object>
            {
                ["UserID"] = userId,
                ["Name"] = m_NameInputField.text,
                ["TeamPosition"] = m_CardPosition.text,
                ["CardTopColor"] = ColorUtility.ToHtmlStringRGB(m_CardTopColor.color),
                ["CardBottomColor"] = ColorUtility.ToHtmlStringRGB(m_CardBottomColor.color),
            };

            userData.Add(item);
        }

        mutableData.Value = userData;
        return TransactionResult.Success(mutableData);
    }

    public void GetProfileInfo()
    {
        m_SaveProfileLoadingBar.SetActive(true);

        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;

        DebugLog("Database Reference: " + reference.Key);

        reference.RunTransaction(GetProfileTransaction)
            .ContinueWithOnMainThread(task => {
              if (task.IsFaulted)
              {
                  DebugLog("GetProfileInfo task.IsFaulted");
              }
              else if (task.IsCanceled)
              {
                  DebugLog("GetProfileInfo task.IsCanceled");
              }
              else if (task.IsCompleted)
              {
                  DebugLog("GetProfileInfo task.IsCompleted");
              }

              m_SaveProfileLoadingBar.SetActive(false);
          });
    }

    private TransactionResult GetProfileTransaction(MutableData mutableData)
    {
        DebugLog("GetProfileTransaction: " + JsonUtility.ToJson(mutableData.Value));
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

                string teamPosition = (string)((Dictionary<string, object>)userData[i])["TeamPosition"];
                string cardTopColor = (string)((Dictionary<string, object>)userData[i])["CardTopColor"];
                string cardBottomColor = (string)((Dictionary<string, object>)userData[i])["CardBottomColor"];
                _ = m_CardTopColor.color;
                ColorUtility.TryParseHtmlString(cardTopColor, out Color topColor);
                m_CardTopColor.color = topColor;
                _ = m_CardBottomColor.color;
                ColorUtility.TryParseHtmlString(cardBottomColor, out Color bottomColor);
                m_CardBottomColor.color = bottomColor;
                int index = m_TeamPositionSelector.elements.IndexOf(teamPosition);
                m_TeamPositionSelector.index = index;
                m_CardPosition.text = m_TeamPositionSelector.elements[index];
                m_CardPosition.color = SetInvertedColor(m_CardBottomColor.color);
                DebugLog(teamPosition);
                DebugLog(cardTopColor);
                DebugLog(cardBottomColor);

                break;
            }
        }

        if (!userExists)
        {
            Dictionary<string, object> item = new Dictionary<string, object>
            {
                ["UserID"] = userId,
                ["Name"] = string.Empty,
                ["TeamPosition"] = "Center",
                ["CardTopColor"] = ColorUtility.ToHtmlStringRGB(Color.black),
                ["CardBottomColor"] = ColorUtility.ToHtmlStringRGB(Color.black),
            };

            userData.Add(item);
            mutableData.Value = userData;
        }

        return TransactionResult.Success(mutableData);
    }

    private void DebugLog(string text)
    {
        Debug.Log(text);
        debugText.text += text + "\n";
    }
}
