using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileTab : MonoBehaviour
{
    [SerializeField] public TMP_InputField m_NameInputField;
    [SerializeField] public HorizontalSelector m_TeamPositionSelector;
    [SerializeField] public TextMeshProUGUI m_CardName;
    [SerializeField] public TextMeshProUGUI m_CardRank;
    [SerializeField] public TextMeshProUGUI m_CardPosition;
    [SerializeField] public Image m_CardTopColor;
    [SerializeField] public Image m_CardBottomColor;
    [SerializeField] public Button m_UploadCardPhotoButton;
    [SerializeField] public RawImage m_CardPhoto;
    [SerializeField] public Button m_SaveProfileButton;

    public static ProfileTab Instance;

    private void Awake()
    {
        Instance = this;
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
}
