namespace NTGame
{
    public class AddTilesItem : ITileItem
    {
        public ItemType ItemType => ItemType.AddTiles;

        public IFactoryOutput Execute(IFactoryInput input)
        {
            var inData = (TileItemInput)input;
            var tileManager = inData.TileManager;
            int spawned = tileManager.SpawnAddTilesBatch();

            return new TileItemOutput
            {
                Success = spawned > 0,
                ConsumeOnExecute = spawned > 0,
                SpawnedCount = spawned,
                Affected = default
            };
        }
    }
}

