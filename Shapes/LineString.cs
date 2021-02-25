using WktToShp.Types;

namespace WktToShp.Shapes
{
	public record LineString : Shape
	{
		public override ShapeType Type => ShapeType.PolyLine;

		public override Box Box => Points.CalculateBox();

		public int[] Parts { get; init; }

		public Types.Point[] Points { get; init; }

		public override int ContenLength => (44 + (4 * Parts.Length) + (16 * Points.Length)) / 2;
	}
}
