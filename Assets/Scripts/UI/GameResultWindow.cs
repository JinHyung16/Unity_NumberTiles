using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NTGame
{
    public class GameResultWindow : BaseWindow
    {
        public interface IListener
        {
            void GoToLobby();
            void GoToStage(GameResultType gameResultType);
        }

        public TMPro.TextMeshProUGUI StageStatusText;
        public TMPro.TextMeshProUGUI GoToStageBtnTxt;
        public TMPro.TextMeshProUGUI GoToStageBtnStageTxt;

        GameResultType _gameResultType;
        IListener _listener;

        public void Open(int stage, GameResultType gameResultType, IListener listener)
        {
            OpenInternal(() =>
            {
                _listener = listener;
                _gameResultType = gameResultType;
                SetText(stage);
            });
        }

        protected override void OnClose()
        {
            _gameResultType = GameResultType.None;
            _listener = null;
        }

        void SetText(int stage)
        {
            var isClearStage = _gameResultType == GameResultType.ClearStage;
            StageStatusText.text = isClearStage ? "클리어!" : "실패";
            GoToStageBtnTxt.text = isClearStage ? "다음 스테이지" : "다시하기";
            GoToStageBtnStageTxt.text = stage.ToString();
        }

        public void OnClickGoToLobby()
        {
            _listener.GoToLobby();
        }

        public void OnClickGotToStage()
        {
            _listener.GoToStage(_gameResultType);
        }
    }
}
