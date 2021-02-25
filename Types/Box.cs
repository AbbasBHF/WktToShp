namespace WktToShp.Types
{
	public record Box
	{
		public Box(Point min, Point max)
			=> (Min, Max) = (min, max);
		public Box(Point center)
			: this(center, center) { }

		public Point Min { get; init; }
		public Point Max { get; init; }
	}
}
