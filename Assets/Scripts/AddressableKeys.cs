namespace NTGame
{
    /// <summary>
    /// 어드레서블 에셋 주소(Address) 모음.
    /// Addressables 로 로드할 때 문자열을 직접 쓰지 말고 여기 상수를 통해 호출한다.
    /// 각 문자열은 Addressables Groups 창의 Address 와 정확히 일치해야 한다.
    /// </summary>
    public static class AddressableKeys
    {
        /// <summary> 풀스크린 윈도우 프리팹 </summary>
        public static class Windows
        {
            public const string Lobby = "lobby_window";
            public const string Tile = "tile_window";
            public const string GameResult = "game_result_window";
        }

        /// <summary> UI 컴포넌트 프리팹 </summary>
        public static class Components
        {
            public const string TileUI = "tile_ui_component";
            public const string ToastMessagePanel = "toast_message_panel";
            public const string StageClear = "stage_clear_component";
            public const string LineClear = "line_clear_component";
            public const string StageItemGroup = "stage_item_group_component";
            public const string StageItemButton = "stage_item_button_component";
            public const string StageItemToggle = "stage_item_toggle_component";
        }
    }
}
