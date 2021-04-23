using System;
using System.IO;
using System.Threading.Tasks;

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
                Console.WriteLine("\tWktToShp <source> [destination] [--no-shx]");
                Console.WriteLine("\tWktToShp <source> [--no-shx]--c=utm2latlng --zn=<zone-number> --zl=<zone-letter>");
                Console.WriteLine("\tWktToShp <source> [destination] [--no-shx] --c=utm2latlng --zn=<zone-number> --zl=<zone-letter>");
                Console.WriteLine("");
                Console.WriteLine("Description:");
                Console.WriteLine("\tsource: Wkt file path");
                Console.WriteLine("\tdestination: Optional. Output file path");
                Console.WriteLine("\t--c: Convert from utm to lat lng");
                return;
            }

            MainAsync(args).Wait();
        }

        private static Task MainAsync(string[] args)
            => Task.Run(async () =>
            {
                var source = args[0];
                var convert = false;
                var zoneNumber = 0;
                var zoneLetter = '0';
                var shpFile = Path.Combine(
                    Path.GetDirectoryName(source),
                    Path.GetFileNameWithoutExtension(source) + ".shp"
                );
                var exportShx = true;

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
                                await Console.Error.WriteLineAsync($"Converting '{item.Substring(4)}' is not supported");
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
                            else if (item == "--no-shx")
                            {
                                exportShx = false;
                            }
                        }
                        else
                        {
                            shpFile = item;
                        }
                    }
                }

                var content = await File.ReadAllTextAsync(source);
                if (content.StartsWith("GEOMETRYCOLLECTION"))
                {
                    await Console.Error.WriteLineAsync("'GEOMETRYCOLLECTION' is not supported");
                    return;
                }

                if (convert && zoneNumber == 0 && zoneLetter == '0')
                {
                    await Console.Error.WriteLineAsync("'zone-number' and 'zone-letter' is not defined");
                    return;
                }

                var shape = content.Trim().ParseShape();

                using var shp = File.OpenWrite(shpFile);

                await shp.Write(9994, true);

                for (var i = 0; i < 5; i++)
                {
                    await shp.Write(0, true);
                }

                await shp.Write(50 + shape.FullLength, true);
                await shp.Write(1000, false);
                await shp.Write((int)shape.Type, false);

                if (convert)
                {
                    await shp.Write(shape.Box, zoneNumber, zoneLetter);
                }
                else
                {
                    await shp.Write(shape.Box);
                }

                for (var i = 0; i < 4; i++)
                {
                    await shp.Write(0.0, true);
                }

                if (convert)
                {
                    await shp.Write(shape, 1, zoneNumber, zoneLetter);
                }
                else
                {
                    await shp.Write(shape, 1);
                }

                if (exportShx)
                {
                    var shxFile = Path.Combine(
                        Path.GetDirectoryName(shpFile),
                        Path.GetFileNameWithoutExtension(shpFile) + ".shx"
                    );

                    using var shx = File.OpenWrite(shxFile);
                    await shx.Write(9994, true);

                    for (var i = 0; i < 5; i++)
                    {
                        await shx.Write(0, true);
                    }

                    await shx.Write(50 + 4 * shape.Count, true);
                    await shx.Write(1000, false);
                    await shx.Write((int)shape.Type, false);
                    (convert ? shx.Write(shape.Box, zoneNumber, zoneLetter) : shx.Write(shape.Box)).Wait();

                    for (var i = 0; i < 4; i++)
                    {
                        await shx.Write(0.0, true);
                    }

                    await shx.Write(shape);
                }
            });
    }
}
