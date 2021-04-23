using System;

namespace WktToShp
{
    public static class UtmLatLngHelper
    {
        public const double A = 6378137;
        public const double EccSquared = 0.00669438;

        public static double ToRadians(this double degree) => degree * Math.PI / 180.0;
        public static double ToDegree(this double radian) => radian / Math.PI * 180.0;

        public static double Floor(this double number) => Math.Floor(number);
        public static double Pow(this double number, double pow) => Math.Pow(number, pow);

        public static int ToInt(this double number) => (int)Math.Round(number);
        public static double ToDouble(this int number) => Convert.ToDouble(number);

        public static bool IsBetween(this double number, double min, double max) => number > min && number < max;
        public static bool IsBetweenOrEqual(this double number, double min, double max) => number >= min && number <= max;
        public static bool IsBetweenOrLeftEqual(this double number, double min, double max) => number >= min && number < max;
        public static bool IsBetweenOrRightEqual(this double number, double min, double max) => number > min && number <= max;

        public static char GetUtmLetterDesignator(this double x) => x switch
        {
            >= 72 and <= 84 => 'X',
            >= 64 and < 72 => 'W',
            >= 56 and < 64 => 'V',
            >= 48 and < 56 => 'U',
            >= 40 and < 48 => 'T',
            >= 32 and < 40 => 'S',
            >= 24 and < 32 => 'R',
            >= 16 and < 24 => 'Q',
            >= 8 and < 16 => 'P',
            >= 0 and < 8 => 'N',
            >= -8 and < 0 => 'M',
            >= -16 and < -8 => 'L',
            >= -24 and < -16 => 'K',
            >= -32 and < -24 => 'J',
            >= -40 and < -32 => 'H',
            >= -48 and < -40 => 'G',
            >= -56 and < -48 => 'F',
            >= -64 and < -56 => 'E',
            >= -72 and < -64 => 'D',
            >= -80 and < -72 => 'C',
            _ => 'Z'
        };

        public static (int easting, int northing, int zoneNumber, char zoneLetter) ToUtm(this Types.Point point)
        {
            double zoneNumber = 0;
            char zoneLetter = 'N';
            double easting = 0;
            double northing = 0;

            var (latitude, longitude) = point;

            var longTemp = longitude;
            var latRad = latitude.ToRadians();
            var longRad = latitude.ToRadians();

            if (longTemp.IsBetweenOrEqual(8, 13) && latitude.IsBetween(54.5, 58))
            {
                zoneNumber = 32;
            }
            else if (latitude.IsBetweenOrLeftEqual(56, 64) && longTemp.IsBetweenOrLeftEqual(3, 12))
            {
                zoneNumber = 32;
            }
            else
            {
                zoneNumber = ((longTemp + 180) / 6) + 1;

                if (latitude.IsBetweenOrLeftEqual(72, 84))
                {
                    if (longTemp.IsBetweenOrLeftEqual(0, 9))
                    {
                        zoneNumber = 31;
                    }
                    else if (longTemp.IsBetweenOrLeftEqual(9, 21))
                    {
                        zoneNumber = 33;
                    }
                    else if (longTemp.IsBetweenOrLeftEqual(21, 33))
                    {
                        zoneNumber = 35;
                    }
                    else if (longTemp.IsBetweenOrLeftEqual(33, 42))
                    {
                        zoneNumber = 37;
                    }
                }
            }
            zoneNumber = zoneNumber.Floor();

            var longOrigin = (zoneNumber - 1) * 6 - 180 + 3;
            var longOriginRad = longOrigin.ToRadians();
            var utmZone = latitude.GetUtmLetterDesignator();
            var eccPrimeSquared = EccSquared / (1 - EccSquared);
            var n = A / Math.Sqrt(1 - EccSquared * Math.Pow(Math.Sin(latRad), 2));
            var t = Math.Pow(Math.Tan(latRad), 2);
            var c = eccPrimeSquared * Math.Pow(Math.Cos(latRad), 2);
            var a = Math.Cos(latRad) * (longRad - longOriginRad);
            var m = A * ((1 - EccSquared / 4 - 3 * EccSquared.Pow(2) / 64 - 5 * EccSquared.Pow(3) / 256) * latRad
            - (3 * EccSquared / 8 + 3 * EccSquared.Pow(2) / 32 + 45 * EccSquared.Pow(3) / 1024) * Math.Sin(2 * latRad)
            + (15 * EccSquared.Pow(2) / 256 + 45 * EccSquared.Pow(3) / 1024) * Math.Sin(4 * latRad)
            - (35 * EccSquared.Pow(3) / 3072) * Math.Sin(6 * latRad));

            easting = 0.9996 * n * (a + (1 - t + c) * a.Pow(3) / 6 + (5 - 18 * t.Pow(3) + 72 * c - 58 * eccPrimeSquared) * a.Pow(5) / 120) + 500000.0;
            northing = 0.9996 * (m + n * Math.Tan(latRad) * (a.Pow(2) / 2 + (5 - t + 9 * c + 4 * c.Pow(2)) * a.Pow(4) / 24 + (61 - 58 * t.Pow(3) + 600 * c - 330 * eccPrimeSquared) * a.Pow(6) / 720));

            if (latitude < 0) northing += 10000000.0;

            return (easting.ToInt(), northing.ToInt(), zoneNumber.ToInt(), zoneLetter);
        }
        public static Types.Point ToLatLng(this Types.Point point, int zoneNumber, char zoneLetter)
        {
            var (easting, northing) = point;
            var e1 = (1 - Math.Sqrt(1 - EccSquared)) / (1 + Math.Sqrt(1 - EccSquared));
            var x = easting - 500000.0;
            var y = northing - (zoneLetter == 'N' ? 0 : 10000000.0);
            var longOrigin = (zoneNumber - 1) * 6 - 180 + 3;
            var eccPrimeSquared = (EccSquared) / (1 - EccSquared);
            var m = y / 0.9996;
            var mu = m / (A * (1 - EccSquared / 4 - 3 * EccSquared.Pow(2) / 64 - 5 * EccSquared.Pow(3) / 256));
            var phi1Rad = mu + (3 * e1 / 2 - 27 * e1.Pow(3) / 32) * Math.Sin(2 * mu) + (21 * e1.Pow(2) / 16 - 55 * e1.Pow(4) / 32) * Math.Sin(4 * mu) + (151 * e1.Pow(3) / 96) * Math.Sin(6 * mu);
            var phi1 = phi1Rad.ToDegree();
            var n1 = A / Math.Sqrt(1 - EccSquared * Math.Sin(phi1Rad).Pow(2));
            var t1 = Math.Tan(phi1Rad).Pow(2);
            var c1 = eccPrimeSquared * Math.Cos(phi1Rad).Pow(2);
            var r1 = A * (1 - EccSquared) / Math.Pow(1 - EccSquared * Math.Sin(phi1Rad).Pow(2), 1.5);
            var d = x / (n1 * 0.9996);
            var lat = (phi1Rad - (n1 * Math.Tan(phi1Rad) / r1) * (d * d / 2 - (5 + 3 * t1 + 10 * c1 - 4 * c1.Pow(2) - 9 * eccPrimeSquared) * d.Pow(4) / 24 + (61 + 90 * t1 + 298 * c1 + 45 * t1.Pow(2) - 252 * eccPrimeSquared - 3 * c1.Pow(2)) * d.Pow(6) / 720)).ToDegree();
            var @long = (d - (1 + 2 * t1 + c1) * d.Pow(3) / 6 + (5 - 2 * c1 + 28 * t1 - 3 * c1.Pow(2) + 8 * eccPrimeSquared + 24 * t1.Pow(2)) * d.Pow(5) / 120) / Math.Cos(phi1Rad);
            @long = longOrigin + @long.ToDegree();

            return (lat, @long);
        }
    }
}
