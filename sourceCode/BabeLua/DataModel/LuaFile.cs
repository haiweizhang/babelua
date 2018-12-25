using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Parsing;

namespace Babe.Lua.DataModel
{
    class LuaFile:IEquatable<LuaFile>
    {
        public string Path { get; private set; }
        public List<LuaMember> Members { get; private set; }
        public TokenList Tokens { get; private set; }
 
        public LuaFile(string path, TokenList tokens) 
        {
            this.Path = path;
            this.Tokens = tokens;
            Members = new List<LuaMember>();
        }

        public void AddTable(LuaTable table)
        {
            this.Members.Add(table);
        }

        public LuaTable GetTable(string name)
        {
            for(int i = 0;i<Members.Count;i++)
            {
                if(Members[i].Name.Equals(name)) return Members[i] as LuaTable;
            }
            return null;
        }

        public bool ContainsTable(string table)
        {
            foreach (LuaMember lt in Members)
            {
                if (lt is LuaTable)
                {
                    if (lt.Name.Equals(table)) return true;
                }
            }
            return false;
        }

        public bool ContainsFunction(string function)
        {
            foreach (LuaMember lf in Members)
            {
                if (lf is LuaFunction)
                {
                    if (lf.Name.Equals(function)) return true;
                }
            }
            
            return false;
        }

        public bool Equals(LuaFile other)
        {
			//return Path.GetFullPath(File).Equals(Path.GetFullPath(other.File));
			return this.Path.Equals(other.Path);
        }

        public override string ToString()
        {
            return System.IO.Path.GetFileName(Path);
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }
    }
}
