using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MoonSharp.Interpreter;

namespace LibPob
{
    public class PobWrapper
    {
        private readonly Script _script = new Script();

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
            _script.DoFile("Assets/PreLaunch.lua");
            _script.DoFile("Launch.lua");

            _script.Globals.Get("launch").Table["CheckForUpdate"] = (Action<bool>) CheckForUpdateHook;
            _script.Globals.Get("launch").Table["DownloadPage"] = (Action<string, Closure, string>) DownloadPageHook;

            _script.DoString("RunCallback(\"OnInit\")");
            _script.DoString("RunCallback(\"OnFrame\")");

            _script.DoFile("Assets/PostLaunch.lua");
        }


        private static void CheckForUpdateHook(bool background)
        {
            // Ignore checking for update... for now
        }

        private static void DownloadPageHook(string url, Closure callback, string cookies)
        {
            // Implement page download 
        }
    }
}
