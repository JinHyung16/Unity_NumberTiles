namespace NTGame
{
    public enum TileNotifyType
    {
        None = 0,
        BoardInit = 1,
        CellOpenChanged = 2,
        CellValueChanged = 3,
        CellSelectedChanged = 4,
        MatchedPair = 5,
        BoardChanged = 6,
        CellCountChanged = 7,
        LineCleared = 8,
        DigitCleared = 9,

        ItemCountChanged = 100
    }

    public struct TileNotify
    {
        public TileNotifyType Type;

        public int Row;
        public int Col;
        public int Row2;
        public int Col2;
        public int Value;
        public bool Flag;

        public ItemType ItemType;
        public int ItemCount;

        public static TileNotify BoardInit(int rows, int cols)
        {
            return new TileNotify
            {
                Type = TileNotifyType.BoardInit,
                Row = rows,
                Col = cols
            };
        }

        public static TileNotify CellCountChanged(int tileCount, int rows, int cols)
        {
            return new TileNotify
            {
                Type = TileNotifyType.CellCountChanged,
                Row = rows,
                Col = cols,
                Value = tileCount
            };
        }
    }

    public interface ITileObserver
    {
        void OnNotify(TileNotify notify);
    }
}

