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

            // Load from file system
            return new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
