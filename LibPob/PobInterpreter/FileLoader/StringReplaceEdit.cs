using System;
using System.Collections.Generic;
using System.Text;

namespace LibPob.PobInterpreter.FileLoader
{
    internal class StringReplaceEdit : IFileEdit
    {
        private readonly string _oldValue;
        private readonly string _newValue;

        public string FileName { get; set; }

        public StringReplaceEdit(string fileName, string oldValue, string newValue)
        {
            FileName = fileName;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public string ApplyEdit(string fileText)
        {
            return fileText.Replace(_oldValue, _newValue);
        }
    }
}
