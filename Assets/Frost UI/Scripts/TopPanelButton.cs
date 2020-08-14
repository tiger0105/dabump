﻿using UnityEngine;
using UnityEngine.EventSystems;

namespace Michsky.UI.Frost
{
    public class TopPanelButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Animator buttonAnimator;

        void Start()
        {
            buttonAnimator = this.GetComponent<Animator>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Input.touchSupported && Input.touches[0].phase == TouchPhase.Began)
            {
                if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Hover to Pressed"))
                {
                    // do nothing because it's clicked
                }

                else
                {
                    buttonAnimator.Play("Hover");
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Input.touchSupported && Input.touches[0].phase == TouchPhase.Began)
            {
                if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Hover to Pressed"))
                {
                    // do nothing because it's clicked
                }

                else
                {
                    buttonAnimator.Play("Normal");
                }
            }
        }
    }
}
