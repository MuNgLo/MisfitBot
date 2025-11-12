using System.Collections.Generic;

namespace ShoutOut{
    internal class DefaultDBLines{
        internal List<string> _lines;

        internal DefaultDBLines(){
            _lines = new List<string>();
            _lines.Add("[USER] coming in with a [EVENT] ([COUNT]) Go check them out. They where last playing [GAME]");
        }
    }// EOF CLASS
}