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
                ConsumeOnExecute = false,
                SpawnedCount = 0,
                Affected = default
            };
        }
    }
}

