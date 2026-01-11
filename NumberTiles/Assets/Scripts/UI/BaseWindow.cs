using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NTGame
{
    public class BaseWindow : MonoBehaviour
    {
        protected void OpenInternal(Action activeBefore, Action activeAfter = null)
        {
            activeBefore?.Invoke();
            gameObject.SetActive(true);
            activeAfter?.Invoke();
        }

        public void Close()
        {
            OnClose();
            gameObject.SetActive(false);
        }

        protected virtual void OnClose()
        {
        }
    }
}
