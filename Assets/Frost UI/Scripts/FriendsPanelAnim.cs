using UnityEngine;

namespace Michsky.UI.Frost
{
    public class FriendsPanelAnim : MonoBehaviour
    {
        private Animator panelAnimator;
        [Header("SETTINGS")]
        public bool isConsole;
        private bool isOpen = false;
        [SerializeField] private RectTransform ExpandIcon;

        void Start()
        {
            panelAnimator = GetComponent<Animator>();
        }

        public void Expand_Collapse()
        {
            if (isOpen == false)
            {
                panelAnimator.Play("Friends In");
                isOpen = true;
                ExpandIcon.transform.eulerAngles = new Vector3(0, 0, 180);
            }
            else
            {
                panelAnimator.Play("Friends Out");
                isOpen = false;
                ExpandIcon.transform.eulerAngles = new Vector3(0, 0, 0);
            }
        }
    }
}