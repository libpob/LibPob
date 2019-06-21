using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace LibPob
{
    class ScriptLoader : ScriptLoaderBase
    {
        private readonly List<string> _rootDirectories;

        private readonly Assembly _resourceAssembly;
        private readonly HashSet<string> _resourceNames;
        private readonly string _namespace;

        public ScriptLoader(params string[] directories)
        {
            if (directories.Any(string.IsNullOrEmpty))
                throw new ArgumentNullException(nameof(directories));
            if (!directories.All(Directory.Exists))
                throw new ArgumentException("Directory does not exist", nameof(directories));

            _rootDirectories = new List<string>(directories);

            _resourceAssembly = Assembly.GetExecutingAssembly();
            _resourceNames = new HashSet<string>(_resourceAssembly.GetManifestResourceNames());
            _namespace = _resourceAssembly.FullName.Split(',').First();
        }

        public override bool ScriptFileExists(string name)
        {
            // Check resources first
            if (_resourceNames.Contains(FileNameToResource(name)))
                return true;

            // Check file system
            return File.Exists(name);
        }

        public override object LoadFile(string file, Table globalContext)
        {
            // Check resources first
            var resource = FileNameToResource(file);
            if (_resourceNames.Contains(resource))
                return _resourceAssembly.GetManifestResourceStream(resource);

            /*
             * God forgive me, for I have made terrible "fixes"
             */
            // TODO: Clean up this entire mess, implement regex matching/replacing
            switch (Path.GetFileName(file))
            {
                /*
                 * TODO: Figure out the problem in MoonSharp
                 *
                 * for k, v in ipairs(a) do
                 *      local b
                 *      assert(a != b, "These should not be equal")
                 * end
                 */
                case "Item.lua":
                    return HackFile(file, new Dictionary<string, string>
                    {
                        {"local varSpec\n", "local varSpec = nil\n"}
                    });

                case "ModStore.lua":
                    return HackFile(file, new Dictionary<string, string>
                    {
                        {"local limitTotal", "local limitTotal = nil"}
                    });

                // TODO: Remove (or shim) PassiveTreeView.lua
                case "PassiveTreeView.lua":
                    return HackFile(file, new Dictionary<string, string>
                    {
                        {"local width = data.width * scale * 1.33", "local width = 1"},
                        {"local height = data.height * scale * 1.33", "local height = 1"}
                    });

                // TODO: Remove (or shim) Control.lua and all related classes
                case "Control.lua":
                    return HackFile(file, new Dictionary<string, string>
                    {
                        {"if type(self[name]) == \"function\" then", "if self[name] and type(self[name]) == \"function\" then"}
                    });

                // Patch out infoDump in Calcs.lua
                case "Calcs.lua":
                    return HackFile(file, new Dictionary<string, string>
                    {
                        {"\t\tinfoDump(env)" , ""}
                    });
            }

            // Load from file system
            return new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        private static Stream HackFile(string file, IDictionary<string, string> values)
        {
            var text = File.ReadAllText(file);
            foreach (var kvp in values)
            {
                text = text.Replace(kvp.Key, kvp.Value);
            }

            var bytes = Encoding.ASCII.GetBytes(text);
            return new MemoryStream(bytes);
        }

        public override string ResolveFileName(string filename, Table globalContext)
        {
            // Check root directories to see if we need to fix up the file name
            foreach (var dir in _rootDirectories)
            {
                var file = Path.Combine(dir, filename);

                if (File.Exists(file))
                    return file;
            }

            // Fallback to the default resolver
            return base.ResolveFileName(filename, globalContext);
        }

        private string FileNameToResource(string file)
        {
            file = file.Replace('/', '.');
            file = file.Replace('\\', '.');
            return $"{_namespace}.{file}";
        }
    }
}
