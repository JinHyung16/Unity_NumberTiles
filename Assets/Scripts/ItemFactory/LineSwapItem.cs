namespace NTGame
{
    public class LineSwapItem : ITileItem
    {
        public ItemType ItemType => ItemType.LineSwap;

        public IFactoryOutput Execute(IFactoryInput input)
        {
            var inData = (TileItemInput)input;
            var tileManager = inData.TileManager;

            bool armed = tileManager.BeginTargetItem(ItemType.LineSwap);
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
