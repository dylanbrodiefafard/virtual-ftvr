using System;

namespace Biglab.Navigation
{
    /// <summary>
    /// An integer based 3D coordinate data-type.
    /// Used to index into a grid volume.
    /// </summary>
    [Serializable]
    public struct Coordinate : IEquatable<Coordinate>
    {
        public int X;
        public int Y;
        public int Z;

        public Coordinate(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override bool Equals(object obj)
        {
            return obj is Coordinate && Equals((Coordinate) obj);
        }

        public bool Equals(Coordinate other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        public override int GetHashCode()
        {
            var hashCode = -307843816;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + Z.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        public static bool operator ==(Coordinate key1, Coordinate key2) => key1.Equals(key2);
        public static bool operator !=(Coordinate key1, Coordinate key2) => !(key1 == key2);
    }
}