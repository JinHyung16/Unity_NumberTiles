using UnityEngine;

namespace NTGame
{
    public class GameManager 
        : SceneSingleton<GameManager>
        , ITileObserver
        , LobbyWindow.IListener
        , TileWindow.IListener
        , GameResultWindow.IListener
    {
        public StageData StageData;
        public Camera UICamera;

        [Header("UI Root")]
        public LobbyWindow LobbyWindow;
        public TileWindow TileWindow;
        public GameResultWindow ResultWindow;

        [Header("Stage")]
        public int StartStageKey = 1;
        int _curStageKey = 1;

        bool _ignoreResultCheck;
        bool _isPlaying;

        protected override void Awake()
        {
            base.Awake();
        }

        void Start()
        {
            _curStageKey = Mathf.Max(1, StartStageKey);

            LobbyWindow.Open(_curStageKey, this);
            TileWindow.Close();
            ResultWindow.Close();
        }

        void StartGame(int stageKey)
        {
            if (StageData == null)
            {
                Debug.LogError("[GameManager] StageData가 연결되어야 합니다.");
                return;
            }

            _curStageKey = Mathf.Max(1, stageKey);

            LobbyWindow.Close();
            ResultWindow.Close();

            var tileManager = TileManager.Instance;
            tileManager.ClearObservers();

            PoolManager.Instance.InitPool();

            TileWindow.Open(this);

            _isPlaying = true;
            _ignoreResultCheck = true;
            tileManager.AddObserver(this);

            if (GameProgressSaver.TryLoad(_curStageKey, out var progress) &&
                tileManager.TryApplyProgress(StageData, _curStageKey, progress))
            {
                _ignoreResultCheck = false;
                TryShowResultIfNeeded();
                return;
            }

            tileManager.StartStage(StageData, _curStageKey);
            _ignoreResultCheck = false;
            TryShowResultIfNeeded();
        }
        void TryShowResultIfNeeded()
        {
            if (_isPlaying == false)
                return;

            if (_ignoreResultCheck)
                return;

            if (ResultWindow != null && ResultWindow.gameObject.activeSelf)
                return;

            var tileManager = TileManager.Instance;
            if (tileManager.IsClearStage())
            {
                ShowResult(GameResultType.ClearStage);
                return;
            }

            if (tileManager.IsFailStage())
            {
                ShowResult(GameResultType.FailStage);
                return;
            }
        }

        void ShowResult(GameResultType gameResultType)
        {
            _isPlaying = false;
            _ignoreResultCheck = true;

            GameProgressSaver.Delete(_curStageKey);

            var tileManager = TileManager.Instance;
            tileManager.RemoveObserver(this);

            if (TileWindow != null)
                TileWindow.Close();

            if (ResultWindow != null)
                ResultWindow.Open(_curStageKey, gameResultType, this);
        }

        void GoToLobby()
        {
            if (ResultWindow != null)
                ResultWindow.Close();

            if (TileWindow != null)
                TileWindow.Close();

            if (LobbyWindow != null)
            {
                LobbyWindow.gameObject.SetActive(true);
                LobbyWindow.Open(_curStageKey, this);
            }

            TileManager.Instance.ClearObservers();
        }

        void ITileObserver.OnNotify(TileNotify notify)
        {
            if (_isPlaying == false)
                return;

            if (_ignoreResultCheck)
                return;

            if (notify.Type == TileNotifyType.BoardChanged ||
                notify.Type == TileNotifyType.ItemCountChanged)
            {
                TryShowResultIfNeeded();
            }
        }

        void LobbyWindow.IListener.StartStage()
        {
            StartGame(_curStageKey);
        }

        void LobbyWindow.IListener.ResetStage()
        {
            GameProgressSaver.Delete(_curStageKey);
            StartGame(_curStageKey);
        }

        void TileWindow.IListener.ExitGame()
        {
            TileManager.Instance.ClearObservers();
            _isPlaying = false;
            _ignoreResultCheck = true;

            TileWindow.Close();
            LobbyWindow.Open(_curStageKey, this);
        }

        void TileWindow.IListener.UseItem(ItemType itemType)
        {
            TileManager.Instance.UseItem(itemType);
        }
 

        void GameResultWindow.IListener.GoToLobby()
        {
            GoToLobby();
        }

        void GameResultWindow.IListener.GoToStage(GameResultType gameResultType)
        {
            if (ResultWindow != null)
                ResultWindow.Close();

            if (gameResultType == GameResultType.ClearStage)
            {
                int nextStageKey = _curStageKey + 1;
                if (StageData != null && StageData.TryGetStage(nextStageKey, out var _))
                {
                    StartGame(nextStageKey);
                    return;
                }

                GoToLobby();
                return;
            }

            StartGame(_curStageKey);
        }
    }
}

