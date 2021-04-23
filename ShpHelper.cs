using System;
using System.IO;
using System.Threading.Tasks;

namespace WktToShp
{
    internal static class ShpHelper
    {
        public static async Task Write(this Stream stream, byte[] buffer, bool bigEndian)
        {
            if (BitConverter.IsLittleEndian == bigEndian)
            {
                Array.Reverse(buffer);
            }

            await stream.WriteAsync(buffer);
        }
        public static async Task Write(this Stream stream, int value, bool bigEndian = false)
            => await stream.Write(BitConverter.GetBytes(value), bigEndian);
        public static async Task Write(this Stream stream, double value, bool bigEndian = false)
            => await stream.Write(BitConverter.GetBytes(value), bigEndian);

        public static async Task Write(this Stream stream, Types.Point point)
        {
            await stream.Write(point.Longitude);
            await stream.Write(point.Latitude);
        }
        public static async Task Write(this Stream stream, Types.Box box)
        {
            await stream.Write(box.Min);
            await stream.Write(box.Max);
        }

        public static async Task Write(this Stream stream, Shapes.Shape shape, int number)
        {
            if (!(shape is Shapes.MultiPolygon))
            {
                await stream.Write(number, true);
                await stream.Write(shape.ContenLength, true);
                await stream.Write((int)shape.Type, false);
            }

            switch (shape)
            {
                case Shapes.Point point: await stream.Write(point); return;
                case Shapes.LineString line: await stream.Write(line); return;
                case Shapes.Polygon polygon: await stream.Write(polygon); return;
                case Shapes.MultiPolygon multiPolygon: await stream.Write(multiPolygon); return;

                default: throw new NotImplementedException();
            }
        }
        public static async Task Write(this Stream stream, Shapes.Shape shape)
        {
            if (shape is Shapes.MultiPolygon multiPolygon)
            {
                var offset = 50;
                foreach (var item in multiPolygon.Polygons)
                {
                    await stream.Write(offset, true);
                    await stream.Write(item.ContenLength, true);
                    offset += item.ContenLength + 2;
                }
            }
            else
            {
                await stream.Write(shape.Offset, true);
                await stream.Write(shape.ContenLength, true);
            }
        }
        public static async Task Write(this Stream stream, Shapes.Point point) => await stream.Write(point.Coordinate);
        public static async Task Write(this Stream stream, Shapes.LineString line)
        {
            await stream.Write(line.Box);
            await stream.Write(line.Parts.Length, false);
            await stream.Write(line.Points.Length, false);

            foreach (var item in line.Parts)
            {
                await stream.Write(item, false);
            }

            foreach (var item in line.Points)
            {
                await stream.Write(item);
            }
        }
        public static async Task Write(this Stream stream, Shapes.Polygon polygon)
        {
            await stream.Write(polygon.Box);
            await stream.Write(polygon.Parts.Length, false);
            await stream.Write(polygon.Points.Length, false);

            foreach (var item in polygon.Parts)
            {
                await stream.Write(item, false);
            }

            foreach (var item in polygon.Points)
            {
                await stream.Write(item);
            }
        }
        public static async Task Write(this Stream stream, Shapes.MultiPolygon multiPolygon)
        {
            var i = 1;
            foreach (var item in multiPolygon.Polygons)
            {
                await stream.Write(item, i++);
            }
        }

        public static async Task Write(this Stream stream, Types.Box box, int zoneNumber, char zoneLetter)
        {
            await stream.Write(box.Min.ToLatLng(zoneNumber, zoneLetter));
            await stream.Write(box.Max.ToLatLng(zoneNumber, zoneLetter));
        }

        public static async Task Write(this Stream stream, Shapes.Shape shape, int number, int zoneNumber, char zoneLetter)
        {
            if (!(shape is Shapes.MultiPolygon))
            {
                await stream.Write(number, true);
                await stream.Write(shape.ContenLength, true);
                await stream.Write((int)shape.Type, false);
            }

            switch (shape)
            {
                case Shapes.Point point: await stream.Write(point, zoneNumber, zoneLetter); return;
                case Shapes.LineString line: await stream.Write(line, zoneNumber, zoneLetter); return;
                case Shapes.Polygon polygon: await stream.Write(polygon, zoneNumber, zoneLetter); return;
                case Shapes.MultiPolygon multiPolygon: await stream.Write(multiPolygon, zoneNumber, zoneLetter); return;

                default: throw new NotImplementedException();
            }
        }
        public static async Task Write(this Stream stream, Shapes.Point point, int zoneNumber, char zoneLetter) => await stream.Write(point.Coordinate.ToLatLng(zoneNumber, zoneLetter));
        public static async Task Write(this Stream stream, Shapes.LineString line, int zoneNumber, char zoneLetter)
        {
            await stream.Write(line.Box, zoneNumber, zoneLetter);
            await stream.Write(line.Parts.Length, false);
            await stream.Write(line.Points.Length, false);

            foreach (var item in line.Parts)
            {
                await stream.Write(item, false);
            }

            foreach (var item in line.Points)
            {
                await stream.Write(item.ToLatLng(zoneNumber, zoneLetter));
            }
        }
        public static async Task Write(this Stream stream, Shapes.Polygon polygon, int zoneNumber, char zoneLetter)
        {
            await stream.Write(polygon.Box, zoneNumber, zoneLetter);
            await stream.Write(polygon.Parts.Length, false);
            await stream.Write(polygon.Points.Length, false);

            foreach (var item in polygon.Parts)
            {
                await stream.Write(item, false);
            }

            foreach (var item in polygon.Points)
            {
                await stream.Write(item.ToLatLng(zoneNumber, zoneLetter));
            }
        }
        public static async Task Write(this Stream stream, Shapes.MultiPolygon multiPolygon, int zoneNumber, char zoneLetter)
        {
            var i = 1;
            foreach (var item in multiPolygon.Polygons)
            {
                await stream.Write(item, i++, zoneNumber, zoneLetter);
            }
        }
    }
}
