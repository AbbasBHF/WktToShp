using System.Linq;
using WktToShp.Types;

namespace WktToShp.Shapes
{
	public record MultiPolygon : Shape
	{
		public override ShapeType Type => ShapeType.Polygon;
		public override Box Box => Polygons.CalculateBox();
		public Polygon[] Polygons { get; init; }
		public override int ContenLength => Polygons?.Sum(x => x.ContenLength) ?? 0;
		public override int Count => Polygons?.Length ?? 0;
		public override int FullLength => Polygons?.Sum(x => x.FullLength) ?? 0;
	}
}
