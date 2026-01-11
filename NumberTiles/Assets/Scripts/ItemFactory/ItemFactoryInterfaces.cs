namespace NTGame
{
    public interface IFactoryInput { }

    public interface IFactoryOutput
    {
        bool Success { get; }
    }

    public interface IItemFactory
    {
        ITileItem Create(ItemType itemType);
    }

    public interface ITileItem
    {
        ItemType ItemType { get; }
        IFactoryOutput Execute(IFactoryInput input);
    }
}

