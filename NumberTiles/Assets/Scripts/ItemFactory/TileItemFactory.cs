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

            // 무조건 1대1 매칭인데, 추후 안정성 높이기 
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

            return null;
        }
    }
}

