using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Babe.Lua.Intellisense
{
    class LuaCompletion : Completion
    {
        public LuaCompletion(string show, string completion, string description, ImageSource icon)
            : base(show, completion, description, icon, "icon")
        {

        }
    }
}
