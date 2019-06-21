using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace LibPob.PobInterpreter.FileLoader
{
    internal class ScriptLoader : ScriptLoaderBase
    {
        private readonly List<string> _rootDirectories;

        private readonly Assembly _resourceAssembly;
        private readonly HashSet<string> _resourceNames;
        private readonly string _namespace;

        public List<IFileEdit> FileEdits { get; } = new List<IFileEdit>();

        public ScriptLoader(string[] directories)
        {
            if (directories.Any(string.IsNullOrEmpty))
                throw new ArgumentNullException(nameof(directories));
            if (!directories.All(Directory.Exists))
                throw new ArgumentException("Directory does not exist", nameof(directories));

            _rootDirectories = new List<string>(directories);

            _resourceAssembly = Assembly.GetExecutingAssembly();
            _resourceNames = new HashSet<string>(_resourceAssembly.GetManifestResourceNames());
            _namespace = _resourceAssembly.FullName.Split(',').First();

            IgnoreLuaPathGlobal = true;
            ModulePaths = BuildModulePaths(directories);
        }

        #region ScriptLoaderBase Overrides
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

            // Apply any needed file edits
            var fileName = Path.GetFileName(file);
            var edits = FileEdits.Where(e => e.FileName == fileName);
            if(edits.Any())
            {
                var text = File.ReadAllText(file);

                foreach (var edit in FileEdits.Where(e => e.FileName == fileName))
                {
                    text = edit.ApplyEdit(text);
                }

                var bytes = Encoding.ASCII.GetBytes(text);
                return new MemoryStream(bytes);
            }


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
        #endregion

        private static string[] BuildModulePaths(IEnumerable<string> directories)
        {
            var paths = new List<string>();
            foreach (var directory in directories)
            {
                paths.Add(Path.Combine(directory, "?"));
                paths.Add(Path.Combine(directory, "?.lua"));
                paths.Add(Path.Combine(directory, "lua", "?"));
                paths.Add(Path.Combine(directory, "lua", "?.lua"));
            }

            return paths.ToArray();
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

        private string FileNameToResource(string file)
        {
            file = file.Replace('/', '.');
            file = file.Replace('\\', '.');
            return $"{_namespace}.{file}";
        }
    }
}
