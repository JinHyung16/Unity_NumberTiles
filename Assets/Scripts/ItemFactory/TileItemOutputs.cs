namespace NTGame
{
    public struct TileItemOutput : IFactoryOutput
    {
        public bool Success { get; set; }
        public bool ConsumeOnExecute { get; set; }
        public int SpawnedCount { get; set; }
        public TileCoordStruct Affected { get; set; }
    }

    public struct TileItemInput : IFactoryInput
    {
        public TileManager TileManager { get; set; }
    }
}

