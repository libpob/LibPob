using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using LibPob;
using MoonSharp.Interpreter.REPL;

namespace Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            var programPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var pobDirectory = Path.Combine(programPath, "PathOfBuilding");

            EnsurePobExists(pobDirectory).GetAwaiter().GetResult();

            var wrapper = new PobWrapper(pobDirectory);
            wrapper.LoadBuildFromFile(Path.Combine(programPath, "Builds", "Test-Character.xml"));

            Console.WriteLine("REPL Ready");
            var repl = new ReplInterpreter(wrapper.Script);

            var line = "";
            while (line != "quit")
            {
                Console.Write(repl.ClassicPrompt);
                line = Console.ReadLine();

                try
                {
                    var result = repl.Evaluate(line);

                    if(result.IsNotNil())
                        Console.WriteLine(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception: {e.Message}");
                }

            }

            /*
            wrapper.RunLua(@"
local build = mainObject.main.modes[""BUILD""]

local f = io.open(""Player.txt"", ""w"")
f:write(inspect(build.calcsTab.mainEnv.player))
f:close()
");
*/

            if (Debugger.IsAttached)
                Console.ReadLine();
        }

        static async Task EnsurePobExists(string directory)
        {
            if (!Directory.Exists(directory))
            {
                await DownloadPob(directory);
                ExtractRuntimeLuaFiles(directory);
                PatchLaunchLua(directory);
            }
        }

        private static async Task DownloadPob(string directory)
        {
            var zipUrl = "https://github.com/Openarl/PathOfBuilding/archive/master.zip";
            var parentDir = new DirectoryInfo(directory).Parent.FullName;

            using (var cli = new HttpClient())
            using (var stream = await cli.GetStreamAsync(zipUrl).ConfigureAwait(false))
            using (var pobZip = new ZipArchive(stream))
            {
                pobZip.ExtractToDirectory(parentDir);
            }

            var masterDir = Path.Combine(parentDir, "PathOfBuilding-master");
            Directory.Move(masterDir, directory);
        }

        private static void ExtractRuntimeLuaFiles(string directory)
        {
            var runtimeFileName = Path.Combine(directory, "runtime-win32.zip");

            using (var fs = File.OpenRead(runtimeFileName))
            using (var runtimeZip = new ZipArchive(fs))
            {
                runtimeZip.ExtractToDirectory(directory);
            }
        }

        static void PatchLaunchLua(string directory)
        {
            var file = Path.Join(directory, "launch.lua");
            var contents = File.ReadAllText(file);
            if (contents.StartsWith("#"))
            {
                contents = "--" + contents;
                File.WriteAllText(file, contents);
            }
        }
    }
}
