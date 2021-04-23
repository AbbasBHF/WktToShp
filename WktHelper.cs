using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WktToShp.Types;

namespace WktToShp
{
    public static class WktHelper
    {
        public static int ParseInt(this string str)
            => int.Parse(str);
        public static double ParseDouble(this string str)
            => double.Parse(str);

        public static Types.Point ParsePoint(this string str)
        {
            var parts = str.Split(" ");
            return new Point(parts[0].ParseDouble(), parts[1].ParseDouble());
        }
        public static Types.Point[] ParseRing(this string str)
        {
            var parts = str.Split(",");
            return parts.Select(x => x.Trim().ParsePoint()).ToArray();
        }

        public static Shapes.Point ParseShapePoint(this string str)
        {
            str = Regex.Replace(str, "^POINT\\s*\\(([^\\)]+)\\)$", "$1");

            return new Shapes.Point
            {
                Coordinate = str.ParsePoint()
            };
        }
        public static Shapes.LineString ParseLineString(this string str)
        {
            str = Regex.Replace(str, "^(MULTI)?LINESTRING\\s*\\((.*)\\)$", "$2");

            if (str.StartsWith("("))
            {
                str = str.Substring(1, str.Length - 2);

                var parts = new List<int>();
                var points = new List<Types.Point>();
                foreach (var part in Regex.Split(str, "\\)\\s*,\\s*\\("))
                {
                    parts.Add(points.Count);
                    points.AddRange(part.ParseRing());
                }

                return new Shapes.LineString
                {
                    Parts = parts.ToArray(),
                    Points = points.ToArray()
                };
            }
            else
            {
                return new Shapes.LineString
                {
                    Parts = new[] { 0 },
                    Points = str.ParseRing()
                };
            }
        }
        public static Shapes.Polygon ParsePolygon(this string str)
        {
            str = Regex.Replace(str, "^POLYGON\\s*\\((.*)\\)$", "$1");
            str = str.Substring(1, str.Length - 2);

            var parts = new List<int>();
            var points = new List<Types.Point>();
            foreach (var part in Regex.Split(str, "\\)\\s*,\\s*\\("))
            {
                parts.Add(points.Count);
                points.AddRange(part.ParseRing());
            }

            return new Shapes.Polygon
            {
                Parts = parts.ToArray(),
                Points = points.ToArray()
            };
        }
        public static Shapes.MultiPolygon ParseMultiPolygon(this string str)
        {
            str = Regex.Replace(str, "^MULTIPOLYGON\\s*\\((.*)\\)$", "$1");
            str = str.Substring(2, str.Length - 4);

            var polygons = new List<Shapes.Polygon>();
            foreach (var item in Regex.Split(str, "\\)\\)\\s*,\\s*\\(\\("))
            {
                polygons.Add($"POLYGON(({item}))".ParsePolygon());
            }

            return new Shapes.MultiPolygon
            {
                Polygons = polygons.ToArray()
            };
        }

        public static Shapes.Shape ParseShape(this string str)
        {
            var type = Regex.Match(str, "^[^\\s\\(]+").Value;

            return type switch
            {
                "POINT" => str.ParseShapePoint(),
                "LINESTRING" => str.ParseLineString(),
                "MULTILINESTRING" => str.ParseLineString(),
                "POLYGON" => str.ParsePolygon(),
                "MULTIPOLYGON" => str.ParseMultiPolygon(),

                _ => throw new Exception($"'{type}' is not supported yet")
            };
        }

        public static Types.Box CalculateBox(this Types.Point[] points)
        {
            if (points?.Length < 1)
            {
                throw new ArgumentNullException("points");
            }

            double? minX = null,
                minY = null,
                maxX = null,
                maxY = null;

            foreach (var item in points)
            {
                if (minX == null)
                {
                    minX = maxX = item.Longitude;
                    minY = maxY = item.Latitude;
                }
                else
                {
                    minX = item.Longitude < minX ? item.Longitude : minX;
                    minY = item.Latitude < minY ? item.Latitude : minY;
                    maxX = item.Longitude > maxX ? item.Longitude : maxX;
                    maxY = item.Latitude > maxY ? item.Latitude : maxY;
                }
            }

            return new Box(
                new Types.Point(minX.Value, minY.Value),
                new Types.Point(maxX.Value, maxY.Value)
            );
        }
        public static Types.Box CalculateBox(this Shapes.Polygon[] polygons)
        {
            if (polygons?.Length < 1)
            {
                throw new ArgumentNullException("points");
            }

            double? minX = null,
                minY = null,
                maxX = null,
                maxY = null;

            foreach (var item in polygons)
            {
                if (minX == null)
                {
                    minX = item.Box.Min.Longitude;
                    minY = item.Box.Min.Latitude;
                    maxX = item.Box.Max.Longitude;
                    maxY = item.Box.Max.Latitude;
                }
                else
                {
                    minX = item.Box.Min.Longitude < minX ? item.Box.Min.Longitude : minX;
                    minY = item.Box.Min.Latitude < minY ? item.Box.Min.Latitude : minY;
                    maxX = item.Box.Max.Longitude > maxX ? item.Box.Max.Longitude : maxX;
                    maxY = item.Box.Max.Latitude > maxY ? item.Box.Max.Latitude : maxY;
                }
            }

            return new Box(
                new Types.Point(minX.Value, minY.Value),
                new Types.Point(maxX.Value, maxY.Value)
            );
        }
    }
}
