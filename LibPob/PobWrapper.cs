using System;
using System.IO;
using System.Net.Http;
using MoonSharp.Interpreter;

namespace LibPob
{
    public class PobWrapper
    {
#if DEBUG
        private readonly Script _script = new Script(CoreModules.Preset_Complete);
#else
        private readonly Script _script = new Script();
#endif

        public string InstallDirectory { get; }

        public PobWrapper(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));
            if (!Directory.Exists(directory))
                throw new ArgumentException("Directory does not exist", nameof(directory));

            InstallDirectory = directory;

            _script.Options.DebugPrint = s => Console.WriteLine($"[Lua] {s}");
            _script.Options.ScriptLoader = new ScriptLoader(InstallDirectory)
            {
                IgnoreLuaPathGlobal = true,
                ModulePaths = new[]
                {
                    Path.Combine(InstallDirectory, "?"),
                    Path.Combine(InstallDirectory, "?.lua"),
                    Path.Combine(InstallDirectory, "lua", "?"),
                    Path.Combine(InstallDirectory, "lua", "?.lua")
                }
            };

            LoadLua();
        }

        private void LoadLua()
        {
            LoadPatches();
            LoadLibs();

            _script.Globals["InstallDirectory"] = InstallDirectory;

            _script.DoFile("Assets/PreLaunch.lua");
            _script.DoFile("Launch.lua");

            _script.Globals.Get("launch").Table["CheckForUpdate"] = (Action<bool>) CheckForUpdateHook;
            _script.Globals.Get("launch").Table["DownloadPage"] = (Action<string, Closure, string>) DownloadPageHook;

            _script.DoString("RunCallback(\"OnInit\")");
            _script.DoString("RunCallback(\"OnFrame\")");

            _script.DoFile("Assets/PostLaunch.lua");
        }

        private void LoadPatches()
        {
            var patchDir = Path.Combine(Utils.AppDirectory, "Patches");

            foreach (var file in Directory.EnumerateFiles(patchDir, "*.lua"))
            {
                _script.DoFile(file);
            }
        }

        private void LoadLibs()
        {
            var libDir = Path.Combine(Utils.AppDirectory, "Libs");

            foreach (var file in Directory.EnumerateFiles(libDir, "*.lua"))
            {
                var module = Path.GetFileNameWithoutExtension(file);

                if (module == null)
                    continue;

                _script.Globals[module] = _script.DoFile(file);
            }
        }

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
    }
}
