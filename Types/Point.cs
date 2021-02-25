namespace WktToShp.Types
{
	public record Point 
	{
		public Point(double longitude, double latitude)
			=> (Longitude, Latitude) = (longitude, latitude);

		public double Latitude { get; init; }
		public double Longitude { get; init; }

		public static implicit operator Point((double lat, double lng) tuple)
			=> new Point(tuple.lng, tuple.lat);

		public void Deconstruct(out double latitude, out double longitude)
			=> (latitude, longitude) = (Latitude, Longitude);
	}
}
