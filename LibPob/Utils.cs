using System;
using System.IO;
using System.Reflection;

namespace LibPob
{
    internal static class Utils
    {
        internal static string AppDirectory
        {
            get
            {
                var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                if(dir == null)
                    throw new Exception("Failed to find AppDirectory");

                return dir;
            }
        }
    }
}
