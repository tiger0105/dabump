using Michsky.UI.ModernUIPack;
using System.Security.Cryptography;
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

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadProfileTab();
    }

    public void LoadProfileTab()
    {
        m_NameInputField.text = PlayerPrefs.GetString("UserName", string.Empty);
        m_CardName.text = m_NameInputField.text;
        m_CardPosition.text = m_TeamPositionSelector.elements[m_TeamPositionSelector.index];
    }

    public void OnNameInputFieldChanged()
    {
        m_CardName.text = m_NameInputField.text;
    }

    public void OnTeamPositionChanged()
    {
        m_CardPosition.text = m_TeamPositionSelector.elements[m_TeamPositionSelector.index];
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
