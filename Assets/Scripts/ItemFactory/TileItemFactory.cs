using System.Collections.Generic;

namespace NTGame
{
    public class TileItemFactory : IItemFactory
    {
        Dictionary<ItemType, ITileItem> _dict = new Dictionary<ItemType, ITileItem>(8);

        public ITileItem Create(ItemType itemType)
        {
            if (_dict.TryGetValue(itemType, out var item))
                return item;

            item = CreateInternal(itemType);
            _dict[itemType] = item;

            return item;
        }

        ITileItem CreateInternal(ItemType itemType)
        {
            if (itemType == ItemType.AddTiles)
                return new AddTilesItem();

            if (itemType == ItemType.BreakOneTile)
                return new BreakOneTileItem();

            if (itemType == ItemType.LineSwap)
                return new LineSwapItem();

            if (itemType == ItemType.DiagonalClear)
                return new DiagonalClearItem();

            return null;
        }
    }
}

