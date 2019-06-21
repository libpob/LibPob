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

        public void LoadBuildFromFile(string file)
        {
            if(string.IsNullOrEmpty(file))
                throw new ArgumentNullException(nameof(file));
            if (!File.Exists(file))
                throw new ArgumentException("File does not exist", nameof(file));

            if (_script.Globals["LoadBuildFromXML"] is Closure func)
            {
                var text = File.ReadAllText(file);

                func.Call(text);
            }
        }

        public void RunLua(string lua)
        {
            if(string.IsNullOrEmpty(lua))
                throw new ArgumentNullException(nameof(lua));

            _script.DoString(lua);
        }

        private void LoadLua()
        {
            LoadPatches();
            LoadLibs();

            _script.Globals["InstallDirectory"] = InstallDirectory;

            _script.DoFile("Assets/PobMockUi.lua");
            _script.DoFile("Launch.lua");

            _script.Globals.Get("launch").Table["CheckForUpdate"] = (Action<bool>) CheckForUpdateHook;
            _script.Globals.Get("launch").Table["DownloadPage"] = (Action<string, Closure, string>) DownloadPageHook;

            if (_script.Globals["RunCallback"] is Closure callback)
            {
                callback.Call("OnInit");
                callback.Call("OnFrame");
            }

            if (_script.Globals.Get("mainObject").Table["promptMsg"] is string error)
            {
                throw new Exception($"PoB Error: {error}");
            }

            _script.DoFile("Assets/HelperFunctions.lua");
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
