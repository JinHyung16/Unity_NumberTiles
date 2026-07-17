namespace NTGame
{
    public enum ItemType
    {
        None = 0,
        AddTiles,
        BreakOneTile,
        LineSwap,
        DiagonalClear,
    }
    public enum GameResultType
    {
        None = 0,
        ClearStage,
        FailStage
    }

    public enum LineSwapHighlightType
    {
        None = 0,
        Primary,
    }

    public enum SoundType
    {
        None = 0,
        TileClick,
        NumberClear,
        RoundClear,
        RoundFail,
    }
}

