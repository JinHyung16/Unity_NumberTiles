namespace NTGame
{
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
