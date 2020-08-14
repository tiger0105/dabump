using Michsky.UI.Frost;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class Home : MonoBehaviour
{
    public static Home Instance;

    [Header("Recently Visited Courts Section")]
    [SerializeField] private GameObject m_CourtCardPrefab;
    [SerializeField] public ScrollRect m_CourtScrollRect;
    [SerializeField] private Transform m_CourtListTransform;
    [Header("Get Started Section")]
    [SerializeField] public GameObject m_GetStartedPanel;
    [SerializeField] public HorizontalScrollSnap m_HorizontalScrollSnap;
    [SerializeField] public Transform m_NavigationBar;

    private void Awake()
    {
        Instance = this;
    }

    public void Navigate(bool isOn)
    {
        if (isOn == true)
        {
            m_HorizontalScrollSnap.GoToScreen(m_NavigationBar.GetComponent<ToggleGroup>().ActiveToggles().First().transform.GetSiblingIndex());
        }
    }

    public void NavigatePage()
    {
        m_NavigationBar.GetChild(m_HorizontalScrollSnap._currentPage).GetComponent<Toggle>().isOn = true;
    }

    public void GetStarted()
    {
        Main.Instance.m_MenuManager.GetComponent<TopPanelManager>().PanelAnim(3);
        Main.Instance.m_MenuManager.GetComponent<BlurManager>().BlurInAnim();
        m_HorizontalScrollSnap.GoToScreen(0);
        m_NavigationBar.GetChild(0).GetComponent<Toggle>().isOn = true;
    }
}
