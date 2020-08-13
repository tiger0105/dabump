using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Michsky.UI.Frost
{
    public class TopPanelManager : MonoBehaviour
    {
        [Header("PANEL LIST")]
        public List<GameObject> panels = new List<GameObject>();

        [Header("BUTTON LIST")]
        public List<GameObject> buttons = new List<GameObject>();

        // [Header("PANEL ANIMS")]
        private string panelFadeIn = "Panel In";
        private string panelFadeOut = "Panel Out";

        // [Header("BUTTON ANIMS")]
        private string buttonFadeIn = "Hover to Pressed";
        private string buttonFadeOut = "Pressed to Normal";

        private GameObject currentPanel;
        private GameObject nextPanel;

        private GameObject currentButton;
        private GameObject nextButton;

        [Header("SETTINGS")]
        public int currentPanelIndex = 0;
        private int currentButtonlIndex = 0;

        private Animator currentPanelAnimator;
        private Animator nextPanelAnimator;

        private Animator currentButtonAnimator;
        private Animator nextButtonAnimator;

        void Start()
        {
            currentButton = buttons[currentPanelIndex];
            currentButtonAnimator = currentButton.GetComponent<Animator>();
            if (currentButtonAnimator != null) currentButtonAnimator.Play(buttonFadeIn);

            currentPanel = panels[currentPanelIndex];
            currentPanelAnimator = currentPanel.GetComponent<Animator>();
            if (currentPanelAnimator != null) currentPanelAnimator.Play(panelFadeIn);
        }

        public void PanelAnim(int newPanel)
        {
            if (newPanel != currentPanelIndex)
            {
                currentPanel = panels[currentPanelIndex];

                currentPanelIndex = newPanel;
                nextPanel = panels[currentPanelIndex];

                currentPanelAnimator = currentPanel.GetComponent<Animator>();
                nextPanelAnimator = nextPanel.GetComponent<Animator>();

                if (currentPanelAnimator != null) currentPanelAnimator.Play(panelFadeOut);
                if (nextPanelAnimator != null) nextPanelAnimator.Play(panelFadeIn);

                currentButton = buttons[currentButtonlIndex];

                currentButtonlIndex = newPanel;
                nextButton = buttons[currentButtonlIndex];

                if (currentButtonAnimator != null) currentButtonAnimator = currentButton.GetComponent<Animator>();
                if (nextButtonAnimator != null) nextButtonAnimator = nextButton.GetComponent<Animator>();

                if (currentButtonAnimator != null) currentButtonAnimator.Play(buttonFadeOut);
                if (nextButtonAnimator != null) nextButtonAnimator.Play(buttonFadeIn);
            }
        }
    }
}