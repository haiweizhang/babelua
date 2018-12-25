using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babe.Lua.Editor
{
    class FindWordTag : TextMarkerTag
    {
        public FindWordTag()
            : base("MarkerFormatDefinition/FindWordFormatDefination")
        { }
    }
}
