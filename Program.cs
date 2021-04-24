using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WktToShp
{
    class Program
    {
        private readonly static int GeometryCollectionLength = "GEOMETRYCOLLECTION".Length + 1;

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

        private static async Task Directory(string path, string[] args)
        {
            var count = 1;
            var errorsCount = 1;
            var files = new DirectoryInfo(path).GetFiles("*.wkt");
            Console.WriteLine($"Total items: {files.Length}");
            var top = Console.CursorTop;
            Console.WriteLine("Converted items: 0");
            var errorsTop = Console.CursorTop;
            Console.WriteLine("Errors: 0");
            foreach (var item in files)
            {
                var process = new Process();
                var arg = new[] { $"\"{item.Name}\"" }.Concat(args).ToArray();
                process.StartInfo = new ProcessStartInfo("WktToShp.exe", $"{string.Join(" ", arg)}")
                {
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WorkingDirectory = path
                };

                var hasErrors = new List<string>();

                process.ErrorDataReceived += async (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        hasErrors.Add(item.Name);
                        await File.AppendAllTextAsync(Path.Combine(path, "errors.log"), $"{item.Name}: {e.Data}\n");
                        Console.SetCursorPosition(8, errorsTop);
                        Console.Write(errorsCount++);
                    }
                };

                try
                {
                    process.Start();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();
                }
                catch (Exception exception)
                {
                    await File.AppendAllTextAsync(Path.Combine(path, "errors.log"), $"{item.Name}: {exception.Message}\n");
                    Console.SetCursorPosition(8, errorsTop);
                    Console.Write(errorsCount++);
                }

                if (!hasErrors.Contains(item.Name))
                {
                    item.Delete();
                }

                Console.SetCursorPosition(17, top);
                Console.Write(count++);
            }

            Console.SetCursorPosition(0, errorsTop + 1);
        }

        private static async Task MultiTypeGeometryCollection(Shapes.GeometryCollection geometryCollection, string name, bool convert, int zoneNumber, char zoneLetter)
        {
            foreach (var group in geometryCollection.GroupedGeometries)
            {
                var shape = group.Value;
                using var shp = File.OpenWrite($"{name}.{group.Key}.shp");

                await shp.Write(9994, true);

                for (var i = 0; i < 5; i++)
                {
                    await shp.Write(0, true);
                }

                await shp.Write(50 + shape.FullLength, true);
                await shp.Write(1000, false);
                await shp.Write((int)shape.Types[0], false);

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

                for (int i = 0; i < shape.Count; i++)
                {
                    var geometry = shape.Geometries.ElementAt(i);
                    if (convert)
                    {
                        await shp.Write(geometry, i + 1, zoneNumber, zoneLetter);
                    }
                    else
                    {
                        await shp.Write(geometry, i + 1);
                    }
                }
            }
        }

        private static async Task GeometryCollection(string content, string shpFile, bool convert, int zoneNumber, char zoneLetter)
        {
            content = Regex.Replace(content, "^\\w+\\s*\\((.+)\\)$", "$1");
            content = Regex.Replace(content, ",\\s*([a-zA-Z]+)", ";$1");
            var shape = content.ParseGeometryCollection();
            if (shape.Types.Length > 1)
            {
                await MultiTypeGeometryCollection(shape, shpFile[..^4], convert, zoneNumber, zoneLetter);
                return;
            }

            using var shp = File.OpenWrite(shpFile);

            await shp.Write(9994, true);

            for (var i = 0; i < 5; i++)
            {
                await shp.Write(0, true);
            }

            await shp.Write(50 + shape.FullLength, true);
            await shp.Write(1000, false);
            await shp.Write((int)shape.Types[0], false);

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

            for (int i = 0; i < shape.Count; i++)
            {
                var geometry = shape.Geometries.ElementAt(i);
                if (convert)
                {
                    await shp.Write(geometry, i + 1, zoneNumber, zoneLetter);
                }
                else
                {
                    await shp.Write(geometry, i + 1);
                }
            }
        }

        private static Task MainAsync(string[] args)
            => Task.Run(async () =>
            {
                var source = args[0];
                var attrs = File.GetAttributes(source);
                if (attrs.HasFlag(FileAttributes.Directory))
                {
                    await Directory(source, args.Skip(1).ToArray());
                    return;
                }

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
                content = content.Trim();
                if (content.StartsWith("GEOMETRYCOLLECTION"))
                {
                    await GeometryCollection(content, shpFile, convert, zoneNumber, zoneLetter);
                    return;
                }

                if (convert && zoneNumber == 0 && zoneLetter == '0')
                {
                    await Console.Error.WriteLineAsync("'zone-number' and 'zone-letter' is not defined");
                    return;
                }

                var shape = content.ParseShape();

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
