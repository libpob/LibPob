using System;
using System.IO;
using LibPob.PobInterpreter;
using MoonSharp.Interpreter;

namespace LibPob
{
    public class PobWrapper
    {
        private readonly PobLuaHost _luaHost;

        public PobWrapper(string pobDirectory)
        {
            if (string.IsNullOrEmpty(pobDirectory))
                throw new ArgumentNullException(nameof(pobDirectory));
            if (!Directory.Exists(pobDirectory))
                throw new ArgumentException("Directory does not exist", nameof(pobDirectory));

            _luaHost = new PobLuaHost(pobDirectory);
        }

        public void LoadBuildFromFile(string file)
        {
            if(string.IsNullOrEmpty(file))
                throw new ArgumentNullException(nameof(file));
            if (!File.Exists(file))
                throw new ArgumentException("File does not exist", nameof(file));

            if (_luaHost.Script.Globals["LoadBuildFromXML"] is Closure func)
            {
                var text = File.ReadAllText(file);

                func.Call(text);
            }
        }

        public void RunLua(string lua)
        {
            if(string.IsNullOrEmpty(lua))
                throw new ArgumentNullException(nameof(lua));

            _luaHost.Script.DoString(lua);
        }
    }
}
