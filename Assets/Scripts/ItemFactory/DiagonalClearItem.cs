namespace NTGame
{
    // 클릭한 셀을 지나는 좌상단 -> 우하단 대각선 위 모든 타일 제거
    public class DiagonalClearItem : ITileItem
    {
        public ItemType ItemType => ItemType.DiagonalClear;

        public IFactoryOutput Execute(IFactoryInput input)
        {
            var inData = (TileItemInput)input;
            var tileManager = inData.TileManager;

            bool armed = tileManager.BeginTargetItem(ItemType.DiagonalClear);
            return new TileItemOutput
            {
                Success = armed,
                ConsumeOnExecute = false,
                SpawnedCount = 0,
                Affected = default
            };
        }
    }
}
