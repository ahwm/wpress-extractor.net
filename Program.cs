using WPress;

var outDir = args.Length > 1 ? args[1] : null;
using var reader = Reader.NewReader(args[0]);
var numFiles = await reader.Extract(outDir);
Console.WriteLine($"{numFiles} file(s) extracted");