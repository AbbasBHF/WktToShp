using WktToShp.Types;

namespace WktToShp.Shapes
{
	public record Point : Shape
	{
		public override ShapeType Type => ShapeType.Point;

		public override Box Box => new Box(Coordinate);

		public Types.Point Coordinate { get; init; }

		public override int ContenLength => 10;
	}
}
