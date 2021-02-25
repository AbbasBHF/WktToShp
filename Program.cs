using System;
using System.IO;

namespace WktToShp
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine("Examples:");
				Console.WriteLine("\tWktToShp <source>");
				Console.WriteLine("\tWktToShp <source> [destination]");
				Console.WriteLine("\tWktToShp <source> --c=utm2latlng --zn=<zone-number> --zl=<zone-letter>");
				Console.WriteLine("\tWktToShp <source> [destination] --c=utm2latlng --zn=<zone-number> --zl=<zone-letter>");
				Console.WriteLine("");
				Console.WriteLine("Description:");
				Console.WriteLine("\tsource: Wkt file path");
				Console.WriteLine("\tdestination: Optional. Output file path");
				Console.WriteLine("\t--c: Convert from utm to lat lng");
				return;
			}

			var source = args[0];
			var convert = false;
			var zoneNumber = 0;
			var zoneLetter = '0';
			var shpFile = Path.Combine(
				Path.GetDirectoryName(source),
				Path.GetFileNameWithoutExtension(source) + ".shp"
			);

			if (args.Length > 1)
			{
				foreach (var item in args[1..])
				{
					if (item.StartsWith("--"))
					{
						if (item.StartsWith("--c=utm2latlng"))
						{
							convert = true;
						}
						else if (item.StartsWith("--c"))
						{
							Console.Error.WriteLine($"Converting '{item.Substring(4)}' is not supported");
							return;
						}
						else if (item.StartsWith("--zn"))
						{
							zoneNumber = int.Parse(item.Substring(5));
						}
						else if (item.StartsWith("--zl"))
						{
							zoneLetter = item[5];
						}
					}
					else
					{
						shpFile = item;
					}

				}
			}

			var content = File.ReadAllText(source);
			if (content.StartsWith("GEOMETRYCOLLECTION"))
			{
				Console.Error.WriteLine("'GEOMETRYCOLLECTION' is not supported");
				return;
			}

			if (convert)
			{
				if (zoneNumber == 0 && zoneLetter == '0')
				{
					Console.Error.WriteLine("'zone-number' and 'zone-letter' is not defined");
					return;
				}
			}

			var shape = content.Trim().ParseShape();
			var shxFile = Path.Combine(
				Path.GetDirectoryName(shpFile),
				Path.GetFileNameWithoutExtension(shpFile) + ".shx"
			);

			using var shp = File.OpenWrite(shpFile);
			using var shx = File.OpenWrite(shxFile);

			shp.Write(9994, true).Wait();
			shx.Write(9994, true).Wait();
			for(var i = 0; i < 5; i++) {
				shp.Write(0, true).Wait();
				shx.Write(0, true).Wait();
			}
			shp.Write(50 + shape.FullLength, true).Wait();
			shx.Write(50 + 4 * shape.Count, true).Wait();
			shp.Write(1000, false).Wait();
			shx.Write(1000, false).Wait();
			shp.Write((int)shape.Type, false).Wait();
			shx.Write((int)shape.Type, false).Wait();
			(convert ? shp.Write(shape.Box, zoneNumber, zoneLetter) : shp.Write(shape.Box)).Wait();
			(convert ? shx.Write(shape.Box, zoneNumber, zoneLetter) : shx.Write(shape.Box)).Wait();
			for(var i = 0; i < 4; i++) {
				shp.Write(0.0, true).Wait();
				shx.Write(0.0, true).Wait();
			}
			(convert ? shp.Write(shape, 1, zoneNumber, zoneLetter) : shp.Write(shape, 1)).Wait();
			shx.Write(shape).Wait();
		}
	}
}
