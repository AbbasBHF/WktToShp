using System.Collections.Generic;
using System.Linq;
using WktToShp.Types;

namespace WktToShp.Shapes
{
    public record GeometryCollection
    {
        public ShapeType[] Types => Geometries.Select(x => x.Type).Distinct().ToArray();

        public Box Box => Geometries.CalculateBox();

        public int Count => Geometries.Count;

        public ICollection<Shape> Geometries { get; init; } = new List<Shape>();

        public Dictionary<ShapeType, GeometryCollection> GroupedGeometries => Geometries.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => new GeometryCollection{ Geometries = x.ToList() });

        public int ContenLength => Geometries.Sum(x => x.ContenLength);
        public  int FullLength => Geometries.Sum(x => x.FullLength);
    }
}
