using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using MoonSharp.Interpreter;

namespace LibPob.PobInterpreter
{
    internal class PobLuaHost
    {
        public Script Script { get; }

        public PobLuaHost(string pobDirectory)
        {
            if (string.IsNullOrEmpty(pobDirectory))
                throw new ArgumentNullException(nameof(pobDirectory));
            if (!Directory.Exists(pobDirectory))
                throw new ArgumentException("Directory does not exist", nameof(pobDirectory));

            Script = new Script(CoreModules.Preset_Complete);

            LoadScriptOptions(pobDirectory);
            LoadPatchesAndLibraries();
            LoadPathOfBuilding(pobDirectory);
        }

        private void LoadScriptOptions(string pobDirectory)
        {
            Script.Options.DebugPrint = s => Console.WriteLine($"[Lua] {s}");
            Script.Options.ScriptLoader = new ScriptLoader(pobDirectory, Path.Combine(Utils.AppDirectory, "PobInterpreter"))
            {
                IgnoreLuaPathGlobal = true,
                ModulePaths = new[]
                {
                    Path.Combine(pobDirectory, "?"),
                    Path.Combine(pobDirectory, "?.lua"),
                    Path.Combine(pobDirectory, "lua", "?"),
                    Path.Combine(pobDirectory, "lua", "?.lua")
                }
            };
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
