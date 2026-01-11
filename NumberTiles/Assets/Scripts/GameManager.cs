using UnityEngine;
using UnityEngine.SceneManagement;

namespace NTGame
{
    // 추후 UI 는 UIManager로 나눠서 Load/Release 관리
    public class GameManager 
        : SceneSingleton<GameManager>
        , ITileObserver
        , LobbyWindow.IListener
        , TileWindow.IListener
        , GameResultWindow.IListener
    {
        public StageData StageData;
        public Camera UICamera;

        [Header("Base Window 모음")]
        public LobbyWindow LobbyWindow;
        public TileWindow TileWindow;
        public GameResultWindow ResultWindow;

        int _curStageKey = 1;
        bool _ignoreResultCheck;
        bool _isPlaying;

        protected override void Awake()
        {
            base.Awake();
        }

        void Start()
        {
            Debug.Assert(StageData != null, "Stage Data 연결이 필요합니다");
            GameMetaSaver.EnsureCreated();

            var startStageKey = 0;

            if (GameProgressSaver.TryFindMostRecentStageKey(out int progressStageKey))
                startStageKey = Mathf.Max(1, progressStageKey);
            else
                startStageKey = GameMetaSaver.GetNextStageAfterClearOrDefault(startStageKey);

            if (StageData.TryGetStage(startStageKey, out _))
                _curStageKey = startStageKey;

            _curStageKey = Mathf.Max(1, _curStageKey);

            LobbyWindow.Open(_curStageKey, this);
            TileWindow.Close();
            ResultWindow.Close();
        }

        void StartGame(int stageKey)
        {
            _curStageKey = Mathf.Max(1, stageKey);
            GameMetaSaver.UpdateLastStage(_curStageKey);

            var tileManager = TileManager.Instance;
            tileManager.ClearObservers();

            LobbyWindow.Close();
            ResultWindow.Close();

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
            if (gameResultType == GameResultType.ClearStage)
                GameMetaSaver.UpdateClearedStage(_curStageKey);

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

        void LobbyWindow.IListener.ClearGameData()
        {
            TileManager.Instance.ClearObservers();
            _isPlaying = false;
            _ignoreResultCheck = true;

            GameProgressSaver.DeleteAll();
            GameMetaSaver.Reset();

            TileWindow.Close();
            ResultWindow.Close();

            var activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
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

