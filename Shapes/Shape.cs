using WktToShp.Types;

namespace WktToShp.Shapes
{
    public record Shape
    {
        public virtual ShapeType Type { get; }
        public virtual Box Box { get; }
        public virtual int ContenLength { get; }
        public virtual int Count { get; } = 1;
        public virtual int Offset { get; } = 50;
        public virtual int FullLength => ContenLength + 4;
    }
}
