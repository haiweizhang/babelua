using Babe.Lua.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Babe.Lua.ToolWindows
{
    class SearchListItem
    {
        public string Id { get; set; }
        public string PathBase { get; set; }
        public string Left { get; set; }
        public string Right { get; set; }
        public string Highlight { get; set; }
        public LuaMember token { get; set; }

        public Brush Background
        {
            get;
            set;
        }

        public SearchListItem() { }

        public SearchListItem(LuaMember lm, string id, string pathbase)
        {
            this.token = lm;
            this.Id = id + ":";

            if (lm.Preview == null)
            {
                Left = lm.ToString();
				Highlight = string.Empty;
				Right = string.Empty;
                PathBase = string.Empty;
            }
            else
            {
                PathBase = pathbase;
                
                //no folder setting contains
                if(string.IsNullOrWhiteSpace(pathbase))
                {
                    Left = string.Format("{0} - ({1},{2}) : {3}", lm.File.Path, lm.Line + 1, lm.Column + 1, lm.Preview.Substring(0, lm.Column).TrimStart());
                }
                //file does not exists in setting folder
                else if (!lm.File.Path.StartsWith(pathbase))
                {
                    PathBase = string.Empty;
                    Left = string.Format("{0} - ({1},{2}) : {3}", lm.File.Path, lm.Line + 1, lm.Column + 1, lm.Preview.Substring(0, lm.Column).TrimStart());
                }
                else
                {
                    Left = string.Format("{0} - ({1},{2}) : {3}", lm.File.Path.Replace(pathbase, ""), lm.Line + 1, lm.Column + 1, lm.Preview.Substring(0, lm.Column).TrimStart());
                }
                Highlight = lm.Name;
                Right = lm.Preview.Substring(lm.Column + lm.Name.Length);
            }
        }

        public SearchListItem(string text, string highlight)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(highlight)) return;
            int index = text.IndexOf(highlight, StringComparison.OrdinalIgnoreCase);
            if (index == -1) return;
            Left = text.Substring(0, index);
            Highlight = highlight;
            Right = text.Substring(index + Highlight.Length);
        }

		public override string ToString()
		{
			return string.Concat(Left, Highlight, Right);
		}
    }
}
