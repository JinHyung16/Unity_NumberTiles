namespace NTGame
{
    public class BreakOneTileItem : ITileItem
    {
        public ItemType ItemType => ItemType.BreakOneTile;

        public IFactoryOutput Execute(IFactoryInput input)
        {
            var inData = (TileItemInput)input;
            var tileManager = inData.TileManager;

            bool armed = tileManager.BeginTargetItem(ItemType.BreakOneTile);
            return new TileItemOutput
            {
                Success = armed,
                ConsumeOnExecute = false, // 실제 소비(카운트 차감)는 타일을 클릭해서 "부쉈을 때" 처리
                SpawnedCount = 0,
                Affected = default
            };
        }
    }
}

