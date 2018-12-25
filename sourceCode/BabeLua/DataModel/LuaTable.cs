using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babe.Lua.DataModel
{
    class LuaTable:LuaMember
    {
        public List<LuaMember> Members { get; private set; }

		public LuaTable(LuaFile file, string name, int line):base(file, name,line, 0)
		{
            Members = new List<LuaMember>();
		}

		public LuaTable(LuaFile file, string basetable, string name, int line)
			: this(file, name, line)
		{
			Father = basetable;
		}

		public string Father { get; private set; }

		public Dictionary<string, List<LuaMember>> GetFullMembers()
		{
			Dictionary<string, List<LuaMember>> Result = new Dictionary<string, List<LuaMember>>();
			Result.Add(this.Name, this.Members);
			//List<LuaMember> Result = new List<LuaMember>(Members);

			var father = Father;
			while (father != null)
			{
				LuaTable Base = IntellisenseHelper.GetTable(father);
				if (Base == null) break;
				//Result.AddRange(Base.Members);
				Result.Add(Base.Name, Base.Members);
				father = Base.Father;
			}

			return Result;
		}
    }
}
