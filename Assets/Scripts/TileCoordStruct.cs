using System;

namespace NTGame
{
    public struct TileCoordStruct : IEquatable<TileCoordStruct>
    {
        public int Row;
        public int Col;

        public bool Equals(TileCoordStruct other)
        {
            return other.Row == Row && other.Col == Col;
        }

        public override bool Equals(object obj)
        {
            return obj is TileCoordStruct other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Helper.CombineHashCode(Row, Col);
        }
    }
}

