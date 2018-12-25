using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babe.Lua.DataModel
{
    class LuaFunction:LuaMember
    {
        public string[] Args { get; private set; }
        public List<LuaMember> Members { get; private set; }

		public LuaFunction(LuaFile file, string name, int line, params string[] args)
			: base(file, name, line, 0)
		{
            this.Args = args;
            this.Members = new List<LuaMember>();
		}

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(Name);
            sb.Append('(');

            if (Args != null && Args.Length > 0)
            {
                foreach (var arg in Args)
                {
                    sb.Append(arg);
                    sb.Append(", ");
                }
                sb.Length = sb.Length - 2;
            }
            
            sb.Append(")");

            return sb.ToString();
        }
    }
}
