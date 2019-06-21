using System;
using System.Collections.Generic;
using System.Text;

namespace LibPob.PobInterpreter.FileLoader
{
    internal interface IFileEdit
    {
        string FileName { get; }

        string ApplyEdit(string fileText);
    }
}
