using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

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
            _script.Globals["bit"] = _script.DoFile("Assets/bitops_lua/funcs.lua");
            _script.Globals["curl_shim"] = _script.DoFile("Assets/curl_shim.lua");
            _script.Globals["PatchJsonToLua"] = (Action<Script>) PatchJsonToLua;

            _script.DoFile("Assets/PreLaunch.lua");
            _script.DoFile("Launch.lua");

            _script.Globals.Get("launch").Table["CheckForUpdate"] = (Action<bool>) CheckForUpdateHook;
            _script.Globals.Get("launch").Table["DownloadPage"] = (Action<string, Closure, string>) DownloadPageHook;

            _script.DoString("RunCallback(\"OnInit\")");
            _script.DoString("RunCallback(\"OnFrame\")");

            _script.DoFile("Assets/PostLaunch.lua");
        }


        private static void PatchJsonToLua(Script script)
        {
            script.Globals["jsonToLua"] = (Func<Script, string, string>) JsonToLua;
        }

        private static string JsonToLua(Script script, string json)
        {
            json = json.Replace('[', '{');
            json = json.Replace(']', '}');
            json = Regex.Replace(json, "\"(-?[0-9]*\\.?[0-9]+)\":", "[$1]=");
            json = Regex.Replace(json, "\"([^\"]+)\":", "[\"$1\"]=");
            json = json.Replace("\\/", "/");

            if (script.Globals["codePointToUTF8"] is Closure codePointToUtf8)
            {
                json = Regex.Replace(json, "[^\u0000-\u007F]", m => codePointToUtf8.Call(m.Value).String);
            }

            return json;
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
