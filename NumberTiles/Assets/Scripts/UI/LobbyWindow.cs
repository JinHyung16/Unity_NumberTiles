using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace NTGame
{
    public class LobbyWindow : BaseWindow
    {
        public interface IListener
        {
            void StartStage();
            void ResetStage();
        }

        public TextMeshProUGUI StageTxt;

        IListener _listener;
        public void Open(int curStage, IListener listener)
        {
            OpenInternal(() =>
            {
                StageTxt.text = curStage.ToString();
                _listener = listener;
            });
        }

        protected override void OnClose()
        {
            _listener = null;
        }

        public void OnClickStartGame()
        {
            _listener.StartStage();
        }

        public void OnClickResetStage()
        {
            _listener.ResetStage();
        }
    }
}
