using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaLanguage.Helper
{
    public interface IWordTable
    {
        void AddTable(string table);
        void RemoveTable(string table);
        bool ContainsTable(string table);
        IEnumerable<string> FilterTable(string table);

        void AddMember(string table, string member);
        void RemoveMember(string table, string member);
        bool ContainsMember(string table, string member);
        IEnumerable<string> GetMembers(string table);

        IEnumerable<string> GetAllGlobal();
        IEnumerable<string> GetAllTable();

        void Clear();
    }

    /// <summary>
    /// 简单的单词表实现。每个单词储存一份。
    /// </summary>
    public class WordTable : IWordTable
    {
        Dictionary<string, HashSet<string>> _table = new Dictionary<string, HashSet<string>>();
        public static List<string> EmptyList = new List<string>();

        public WordTable()
        {
            _table.Add("_G", new HashSet<string>());
        }

        public virtual HashSet<string> this[string index]
        {
            get
            {
                if (!ContainsTable(index)) _table.Add(index, new HashSet<string>());
                return _table[index];
            }
            set
            {
                if (!ContainsTable(index)) _table.Add(index, value);
                else _table[index] = value;
            }
        }

        public virtual void AddGlobal(string member)
        {
            this.AddMember("_G", member);
        }

        public virtual IEnumerable<string> FilterGlobal(string member)
        {
            var ret = from mem in this["_G"] where mem.Filter(member) select mem;
            return ret == null ? EmptyList : ret;
        }

        #region IWordTable
        public virtual void AddTable(string table)
        {
            if (!_table.ContainsKey(table))
            {
                _table.Add(table, new HashSet<string>());
            }
        }

        public virtual void RemoveTable(string table)
        {
            if (_table.ContainsKey(table))
            {
                _table.Remove(table);
            }
        }

        public virtual bool ContainsTable(string table)
        {
            return _table.ContainsKey(table);
        }

        public virtual IEnumerable<string> FilterTable(string table)
        {
            var ret = from tb in _table.Keys where tb.Filter(table) select tb;
            return ret == null ? EmptyList : ret;
        }

        public virtual void AddMember(string table, string member)
        {
            if (_table.ContainsKey(table) && !_table[table].Contains(member))
            {
                _table[table].Add(member);
            }
        }

        public virtual void RemoveMember(string table, string member)
        {
            if (_table.ContainsKey(table) && _table[table].Contains(member))
            {
                _table[table].Remove(member);
            }
        }

        public virtual bool ContainsMember(string table, string member)
        {
            return _table.ContainsKey(table) && _table[table].Contains(member);
        }

        public virtual IEnumerable<string> GetMembers(string table)
        {
            if (!ContainsTable(table)) return EmptyList;
            return _table[table];
        }

        public void Clear()
        {
            _table.Clear();
            _table.Add("_G", new HashSet<string>());
        }

        public IEnumerable<string> GetAllGlobal()
        {
            return _table["_G"];
        }

        public IEnumerable<string> GetAllTable()
        {
            return _table.Keys;
        }
        #endregion
    }

    /// <summary>
    /// Lua内置table和function
    /// </summary>
    class LuaKeyWords : WordTable
    {
        private LuaKeyWords()
        {
            this["_G"] = new HashSet<string>() { "assert", "collectgarbage", "dofile", "error", "getfenv", "getmetatable", "ipairs", "load", "loadfile", "loadstring", "module", "next", "pairs", "pcall", "print", "rawequal", "rawget", "rawset", "require", "select", "setfenv", "setmetatable", "tonumber", "tostring", "type", "unpack", "xpcall", "class", "new", "delete" };
            this["coroutine"] = new HashSet<string>() { "create", "resume", "running", "status", "wrap", "yield", };
            this["debug"] = new HashSet<string>() { "debug", "getfenv", "gethook", "getinfo", "getlocal", "getmetatable", "getregistry", "getupvalue", "setfenv", "sethook", "setlocal", "setmetatable", "setupvalue", "traceback", };
            this["io"] = new HashSet<string>() { "close", "flush", "input", "lines", "open", "output", "popen", "read", "stderr", "stdin", "stdout", "tmpfile", "type", "write", };
            this["math"] = new HashSet<string>() { "abs", "acos", "asin", "atan", "atan2", "ceil", "cos", "cosh", "deg", "exp", "floor", "fmod", "frexp", "huge", "ldexp", "log", "log10", "max", "min", "modf", "pi", "pow", "rad", "random", "randomseed", "sin", "sinh", "sqrt", "tan", "tanh", };
            this["os"] = new HashSet<string>() { "clock", "date", "difftime", "execute", "exit", "getenv", "remove", "rename", "setlocale", "time", "tmpname", };
            this["package"] = new HashSet<string>() { "cpath", "loaded", "loaders", "loadlib", "path", "preload", "seeall", };
            this["string"] = new HashSet<string>() { "byte", "char", "dump", "find", "format", "gmatch", "gsub", "len", "lower", "match", "rep", "reverse", "sub", "upper", };
            this["table"] = new HashSet<string>() { "concat", "sort", "maxn", "remove", "insert", };
        }

        static LuaKeyWords _instance;
        public static LuaKeyWords Instance
        {
            get
            {
                if (_instance == null) _instance = new LuaKeyWords();
                return _instance;
            }
        }
    }

    class ProjectWordTable
    {
        List<FileWordTable> tables;
        public List<FileWordTable> Files
        {
            get
            {
                return tables;
            }
        }

        public FileWordTable this[string index]
        {
            get
            {
                foreach (var tb in tables)
                {
                    if (tb.FileName == index) return tb;
                }
                return null;
            }
            set
            {
                for (int i = 0; i < tables.Count; i++)
                {
                    if (tables[i].FileName == index)
                    {
                        tables[i] = value;
                        return;
                    }
                }
                tables.Add(value);
            }
        }

        public ProjectWordTable()
        {
            tables = new List<FileWordTable>();
        }

        public FileWordTable AddFile(string file)
        {
            foreach (var tb in tables)
            {
                if (tb.FileName == file) return tb;
            }
            var t = new FileWordTable(file);
            tables.Add(t);
            return t;
        }

        public void RemoveFile(string file)
        {
            FileWordTable fwt = null;
            foreach (var tb in tables)
            {
                if (tb.FileName == file)
                {
                    fwt = tb;
                    return;
                }
            }
            tables.Remove(fwt);
        }

        public bool ContainsFile(string file)
        {
            return this[file] != null;
        }

        public bool ContainsTable(string table)
        {
            foreach (var tb in tables.ToArray())
            {
                if(tb.ContainsTable(table))  return true;
            }
            return false;
        }

        public IEnumerable<string> FilterTable(string table)
        {
            List<string> ret = new List<string>();

            foreach (var tb in tables.ToArray())
            {
                ret.AddRange(tb.FilterTable(table));
            }
            return ret.Distinct();
        }

        public IEnumerable<string> FilterGlobal(string word)
        {
            List<string> ret = new List<string>();
            foreach (var tb in tables.ToArray())
            {
                ret.AddRange(tb.FilterGlobal(word));
            }
            return ret.Distinct();
        }

        public bool ContainsMember(string table, string member)
        {
            foreach (var f in tables.ToArray())
            {
                if (f.ContainsMember(table, member)) return true;
            }
            return false;
        }

        public IEnumerable<string> GetMembers(string table)
        {
            foreach (var tb in tables.ToArray())
            {
                if (tb.ContainsTable(table)) return tb.GetMembers(table);
            }
            return WordTable.EmptyList;
        }

        public IEnumerable<string> GetAllGlobals()
        {
            List<string> ret = new List<string>();
            foreach (var tb in tables.ToArray())
            {
                ret.AddRange(tb.GetAllGlobal());
            }
            return ret.Distinct();
        }

        public IEnumerable<string> GetAllTables()
        {
            List<string> ret = new List<string>();
            foreach (var tb in tables.ToArray())
            {
                ret.AddRange(tb.GetAllTable());
            }
            return ret.Distinct();
        }
    }

    /// <summary>
    /// 请不要跨文件定义table。
    /// </summary>
    public class FileWordTable : WordTable
    {
        public string FileName;

        public FileWordTable(string file)
        {
            this.FileName = file;
        }

        public override string ToString()
        {
            return Path.GetFileName(FileName);
        }
    }

    /// <summary>
    /// 考虑把单词表分为3部分：系统内置（lua关键字）、Babe框架、用户
    /// 系统内置全局保留一份，Babe框架按项目隔离，用户表按文件隔离
    /// 把三种表综合起来，提供对外使用的接口。
    /// 此类型需要项目隔离。
    /// </summary>
    public class WordTableHelper
    {
        ProjectWordTable _tables;
        internal ProjectWordTable ProjectWordTable
        {
            get
            {
                return _tables;
            }
        }

        string[] mykeywords = {"and","break","do","else","elseif",
                                "end","false","for","function","if",
                                "in","local","nil","not","or",
                                "repeat","return","then","true","until","while"};

        FileWordTable _currentfile;
        public string CurrentFile
        {
            get
            {
                return _currentfile == null ? null : _currentfile.FileName;
            }
            set
            {
                if (!_tables.ContainsFile(value))
                {
                    _tables.AddFile(value);
                }
                _currentfile = _tables[value];
            }
        }

        public WordTableHelper()
        {
            _tables = new ProjectWordTable();
        }

        public FileWordTable GetCurrent()
        {
            return _currentfile;
        }

        public FileWordTable AddFile(string file)
        {
            return _tables.AddFile(file);
        }

        public void AddTable(string table)
        {
            _currentfile.AddTable(table);
        }

        public void RemoveTable(string table)
        {
            _currentfile.RemoveTable(table);
        }

        public bool ContainsTable(string table)
        {
            return _tables.ContainsTable(table);
        }

        public bool ContainsGlobal(string word)
        {
            return _tables.ContainsMember("_G",word) || LuaKeyWords.Instance.ContainsMember("_G", word);
        }

        public IEnumerable<string> FilterTable(string table)
        {
            return _tables.FilterTable(table).Concat(LuaKeyWords.Instance.FilterTable(table)).Distinct();
        }

        public IEnumerable<string> FilterGlobal(string word)
        {
            var tb = _tables.FilterGlobal(word);
            var lk = LuaKeyWords.Instance.FilterGlobal(word);
            var mk = from st in mykeywords where st.Filter(word) select st;
            if (mk == null) return tb.Concat(lk).Distinct();

            return tb.Concat(lk).Concat(mk).Distinct();
        }

        public IEnumerable<string> GetAllTables()
        {
            return LuaKeyWords.Instance.GetAllTable().Concat(_tables.GetAllTables());
        }

        public IEnumerable<string> GetAllGlobals()
        {
            return _tables.GetAllGlobals().Concat(LuaKeyWords.Instance.GetAllGlobal()).Concat(mykeywords).Distinct();
        }

        public void AddMember(string table, string member)
        {
            _currentfile.AddMember(table, member);
        }

        public void RemoveMember(string table, string member)
        {
            _currentfile.RemoveMember(table, member);
        }

        public IEnumerable<string> GetMembers(string table)
        {
            if (_tables.ContainsTable(table)) return _tables.GetMembers(table);
            else return LuaKeyWords.Instance.GetMembers(table);
        }

        public string GetTableFunctionComment(string table, string function)
        {
            return "Lua Function";
        }
    }
}
