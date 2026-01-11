using System.Collections.Generic;

namespace NTGame
{
    public class TileItemFactory : IItemFactory
    {
        readonly Dictionary<ItemType, ITileItem> _cache = new Dictionary<ItemType, ITileItem>(8);

        public ITileItem Create(ItemType itemType)
        {
            if (_cache.TryGetValue(itemType, out var item))
                return item;

            item = CreateInternal(itemType);
            if (item != null)
                _cache[itemType] = item;

            return item;
        }

        ITileItem CreateInternal(ItemType itemType)
        {
            if (itemType == ItemType.AddTiles) 
                return new AddTilesItem();

            if (itemType == ItemType.BreakOneTile) 
                return new BreakOneTileItem();

            return null;
        }
    }
}

