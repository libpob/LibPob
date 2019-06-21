using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using LibPob.PobInterpreter.FileLoader;
using MoonSharp.Interpreter;

namespace LibPob.PobInterpreter
{
    internal class PobLuaHost
    {
        private readonly ScriptLoader _scriptLoader;

        public Script Script { get; }

        public PobLuaHost(string pobDirectory)
        {
            if (string.IsNullOrEmpty(pobDirectory))
                throw new ArgumentNullException(nameof(pobDirectory));
            if (!Directory.Exists(pobDirectory))
                throw new ArgumentException("Directory does not exist", nameof(pobDirectory));

            _scriptLoader = new ScriptLoader(new[]
            {
                pobDirectory,
                Path.Combine(Utils.AppDirectory, "PobInterpreter")
            });

            Script = new Script(CoreModules.Preset_Complete)
            {
                Options =
                {
                    DebugPrint = s => Console.WriteLine($"[Lua] {s}"),
                    ScriptLoader = _scriptLoader
                }
            };

            ApplyFileEdits();
            LoadPatchesAndLibraries();
            LoadPathOfBuilding(pobDirectory);
        }


        private void ApplyFileEdits()
        {
            _scriptLoader.FileEdits.AddRange(new IFileEdit[]
            {
                /*
                 * TODO: Figure out the problem in MoonSharp
                 *
                 * for k, v in ipairs(a) do
                 *      local b
                 *      assert(a != b, "These should not be equal")
                 * end
                 */
                new StringReplaceEdit("Item.lua", 
                    "local varSpec\n", 
                    "local varSpec = nil\n"),
                new StringReplaceEdit("ModStore.lua", 
                    "local limitTotal", 
                    "local limitTotal = nil"),

                // TODO: Shim PassiveTreeView.lua
                new StringReplaceEdit("PassiveTreeView.lua", 
                    "local width = data.width * scale * 1.33",
                    "local width = 1"),
                new StringReplaceEdit("PassiveTreeView.lua", 
                    "local height = data.height * scale * 1.33",
                    "local height = 1"),

                // TODO: Shim Control.lua and related classes
                new StringReplaceEdit("Control.lua",
                    "if type(self[name]) == \"function\" then",
                    "if self[name] and type(self[name]) == \"function\" then"),

                // Patch out infoDump in Calcs.lua to clean up Console outut
                new StringReplaceEdit("Calcs.lua", 
                    "\t\tinfoDump(env)",
                    "")
            });
        }

        private void LoadPatchesAndLibraries()
        {
            // Patches
            var patchDir = Path.Combine(Utils.AppDirectory, "PobInterpreter", "Patches");
            foreach (var file in Directory.EnumerateFiles(patchDir, "*.lua"))
            {
                Script.DoFile(file);
            }

            // Libraries, set global name equal to file name
            var libDir = Path.Combine(Utils.AppDirectory, "PobInterpreter", "Libs");
            foreach (var file in Directory.EnumerateFiles(libDir, "*.lua"))
            {
                var module = Path.GetFileNameWithoutExtension(file);

                if (module == null)
                    continue;

                Script.Globals[module] = Script.DoFile(file);
            }
        }

        private void LoadPathOfBuilding(string pobDirectory)
        {
            Script.Globals["InstallDirectory"] = pobDirectory;

            // Load mock ui and pob's launch.lua
            Script.DoFile("PobInterpreter/Assets/PobMockUi.lua");
            Script.DoFile("Launch.lua");

            // Hook launch functions
            Script.Globals.Get("launch").Table["CheckForUpdate"] = (Action<bool>)CheckForUpdateHook;
            Script.Globals.Get("launch").Table["DownloadPage"] = (Action<string, Closure, string>)DownloadPageHook;

            // Run initial callbacks for setup
            if (Script.Globals["RunCallback"] is Closure callback)
            {
                using (PerfTimer.Start("RunCallback(OnInit)"))
                    callback.Call("OnInit");

                using (PerfTimer.Start("RunCallback(OnFrame)"))
                    callback.Call("OnFrame");

                Script.DoString(@"print(""PoB Loaded"")");
            }

            // Check for errors, this should be done more often
            if (Script.Globals.Get("mainObject").Table["promptMsg"] is string error)
            {
                throw new Exception($"PoB Error: {error}");
            }

            // Load some helper function for interacting with PoB
            Script.DoFile("PobInterpreter/Assets/HelperFunctions.lua");
        }

        #region PoB Lua Hooks
        private static void CheckForUpdateHook(bool background)
        {
            // Ignore checking for update... for now
        }

        private static void DownloadPageHook(string url, Closure callback, string cookies)
        {
            // Poor practice, HttpClient should be one static version per app.
            // Shouldn't matter too much, it's barely used.
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(url).Result;
                var content = response.Content.ReadAsStringAsync().Result;

                callback.Call(content);
            }
        }
        #endregion
    }
}
