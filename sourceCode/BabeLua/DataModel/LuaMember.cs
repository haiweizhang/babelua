using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babe.Lua.DataModel
{
    class LuaMember : IEquatable<LuaMember>, IComparable<LuaMember>
    {
        public string Name { get; protected set; }

        public string Comment { get; set; }

        //所在的位置
        public LuaFile File { get; protected set; }
		public int Line { get; protected set; }
		public int Column { get; protected set; }
		
        //Preview只用于查找引用的用途
		public string Preview { get; set; }

        public LuaMember(LuaFile file, string name, int line, int column)
        {
            this.File = file;
            this.Name = name;
            this.Line = line;
            this.Column = column;
        }

        public LuaMember(LuaFile file, Token token):
            this(file, token.ValueString, token.Location.Line, token.Location.Column)
        {
        }

        public bool Equals(LuaMember other)
        {
            return other.Name.Equals(this.Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        public LuaMember Copy()
        {
            LuaMember mem = new LuaMember(this.File, this.Name, this.Line, this.Column);
            return mem;
        }

        public int CompareTo(LuaMember other)
        {
            return this.Name.CompareTo(other.Name);
        }
    }
}
