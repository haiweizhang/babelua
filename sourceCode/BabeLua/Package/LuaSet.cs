using Babe.Lua.Editor;

namespace Babe.Lua.Package
{
	public class LuaSet
	{
		public string Name;
		public string Folder;
		public string LuaExecutable;
		public string WorkingPath;
		public string CommandLine;
		public EncodingName Encoding;

		public LuaSet() { }

		public LuaSet(string LuaPath, string LuaExecutable, string WorkingPath, string CommandLine, EncodingName Encoding)
		{
			this.Folder = LuaPath;
			this.LuaExecutable = LuaExecutable;
			this.CommandLine = CommandLine;
			this.Encoding = Encoding;
			this.WorkingPath = WorkingPath;
		}

		public LuaSet(string Name, string LuaPath, string LuaExecutable = null, string WorkingPath = null, string CommandLine = null, EncodingName Encoding = EncodingName.UTF8)
			: this(LuaPath, LuaExecutable, WorkingPath, CommandLine, Encoding)
		{
			this.Name = Name;
		}
	}
}
