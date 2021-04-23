using WktToShp.Types;

namespace WktToShp.Shapes
{
    public record Polygon : Shape
    {
        public override ShapeType Type => ShapeType.Polygon;

        public override Box Box => Points.CalculateBox();

        public int[] Parts { get; init; }

        public Types.Point[] Points { get; init; }

        public override int ContenLength => (44 + (4 * Parts.Length) + (16 * Points.Length)) / 2;
    }
}
