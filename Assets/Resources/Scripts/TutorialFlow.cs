using DigitalRubyShared;
using UnityEngine;
using UnityEngine.UI;

public class TutorialFlow : MonoBehaviour
{
    public Sprite[] tutorialScreens;
    private int screenIndex = 0;
    public bool forceTutorial = false;

    public Image[] unfilledDots;
    public RectTransform panelRoot;
    private Vector3 panelRootTarget;

    private void Start()
    {
        swipe = new SwipeGestureRecognizer();
        swipe.StateUpdated += Swipe_Updated;
        swipe.DirectionThreshold = 0;
        swipe.MinimumNumberOfTouchesToTrack = swipe.MaximumNumberOfTouchesToTrack = SwipeTouchCount;
        swipe.PlatformSpecificView = Image;
        FingersScript.Instance.AddGesture(swipe);
        TapGestureRecognizer tap = new TapGestureRecognizer();
        tap.StateUpdated += Tap_Updated;
        FingersScript.Instance.AddGesture(tap);

        ShowScreen(0);
    }

    private void Update()
    {
        swipe.MinimumNumberOfTouchesToTrack = swipe.MaximumNumberOfTouchesToTrack = SwipeTouchCount;
        swipe.EndMode = SwipeMode;
        panelRoot.localPosition = Vector3.Lerp(panelRoot.localPosition, panelRootTarget, Time.deltaTime * 10f);
    }

    private void ShowScreen(int index)
    {
        foreach (Image i in unfilledDots)
        {
            i.enabled = false;
        }
        unfilledDots[index].enabled = true;

        panelRootTarget = Vector3.right * (panelRoot.rect.width / 9f) + Vector3.left * panelRoot.rect.width / 9f * index;

    }


    [Tooltip("Set the required touches for the swipe.")]
    [Range(1, 10)]
    public int SwipeTouchCount = 1;

    [Tooltip("Controls how the swipe gesture ends. See SwipeGestureRecognizerSwipeMode enum for more details.")]
    public SwipeGestureRecognizerEndMode SwipeMode = SwipeGestureRecognizerEndMode.EndImmediately;

    public GameObject Image;

    private SwipeGestureRecognizer swipe;


    private void Tap_Updated(GestureRecognizer gesture)
    {
        if (gesture.State == GestureRecognizerState.Ended)
        {
            Debug.Log("Tap");
        }
    }

    private void Swipe_Updated(GestureRecognizer gesture)
    {
        SwipeGestureRecognizer swipe = gesture as SwipeGestureRecognizer;
        if (swipe.State == GestureRecognizerState.Ended)
        {
            switch (swipe.EndDirection)
            {
                case SwipeGestureRecognizerDirection.Left:
                    SwipeLeft();
                    break;
                case SwipeGestureRecognizerDirection.Right:
                    SwipeRight();
                    break;
            }
        }
    }

    public void GoToProfilePage()
    {

    }

    private void SwipeLeft()
    {
        screenIndex++;
        if (screenIndex > 9)
        {
            GoToProfilePage();
            Destroy(gameObject);
            return;
        }
        screenIndex = Mathf.Clamp(screenIndex, 0, 2);
        ShowScreen(screenIndex);
    }

    private void SwipeRight()
    {
        screenIndex--;
        screenIndex = Mathf.Clamp(screenIndex, 0, 2);
        ShowScreen(screenIndex);
    }
}
