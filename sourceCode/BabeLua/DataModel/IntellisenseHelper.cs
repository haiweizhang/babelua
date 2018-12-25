using Babe.Lua;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Babe.Lua.Package;

namespace Babe.Lua.DataModel
{
    /// <summary>
    /// 此对象需要项目间隔离
    /// </summary>
    class IntellisenseHelper
    {
        static FileManager FileManager = FileManager.Instance;
        //static LuaFile InnerTables = LuaInnerTable.Instance;
        static LuaFile InnerTables = OuterCompletionData.Instance;

        static bool isScanning = false;
        static bool isBreak = false;
        static Thread _thread;

        //static FileSystemWatcher FileWatcher = new FileSystemWatcher();

        public static void Scan()
        {
            var set = BabePackage.Current.CurrentSetting;

            if (set == null || string.IsNullOrWhiteSpace(set.Folder) || !Directory.Exists(set.Folder))
            {
                //FileWatcher.EnableRaisingEvents = false;
                FileManager.ClearData();
                return;
            }

            //FileWatcher.Filter = "*.lua";
            //FileWatcher.IncludeSubdirectories = true;
            //FileWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
            //FileWatcher.Changed += FileWatcher_Changed;
            //FileWatcher.Path = set.Folder;
            //FileWatcher.EnableRaisingEvents = true;

            Stop();

            _thread = new Thread(Work);
			_thread.IsBackground = true;
            _thread.Start();
        }

        //static void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        //{
        //    if (!File.Exists(e.FullPath)) return;
        //    if (!FileManager.Files.Contains(new LuaFile(e.FullPath, null))) return;
        //    new TreeParser().HandleFile(e.FullPath);
        //}

        public static void Stop()
        {
            if (isScanning)
            {
                isBreak = true;

                if (_thread != null && _thread.IsAlive)
                    _thread.Join();
            }
        }

        static void Work()
        {
            FileManager.ClearData();

            isScanning = true;
            isBreak = false;

			try
			{
				//var names = System.IO.Directory.EnumerateFiles(BabePackage.Current.CurrentSetting.Folder, "*.lua", System.IO.SearchOption.AllDirectories).Where((name) => { return name.ToLower().EndsWith(".lua"); });
				var names = new List<string>();
				EnumFiles(BabePackage.Current.CurrentSetting.Folder, "*.lua", names);
				int count = 0;

				foreach (var name in names)
				{
					if (isBreak)
					{
						isBreak = false;
						return;
					}
					var tp = new TreeParser();
					tp.HandleFile(name);
					BabePackage.DTEHelper.GetStatusBar().Progress(true, "scan", ++count, names.Count());
				}
			}
			finally
			{
                isScanning = false;
                BabePackage.DTEHelper.GetStatusBar().Progress(false);

                BabePackage.WindowManager.RefreshOutlineWnd();
                GC.Collect();
			}
        }

		static void EnumFiles(string folder, string pattern, List<string> list)
		{
			try
			{
				list.AddRange(Directory.EnumerateFiles(folder, pattern));

				foreach (var subfolder in Directory.EnumerateDirectories(folder))
				{
					EnumFiles(subfolder, pattern, list);
				}
			}
			catch { }
		}

        public static void Refresh(Irony.Parsing.ParseTree tree)
        {
            if (isScanning) return;

            var file = FileManager.CurrentFile;
            if (file == null || BabePackage.DTEHelper.DTE.ActiveDocument == null || file.Path != BabePackage.DTEHelper.DTE.ActiveDocument.FullName) return;

            if (System.IO.File.Exists(file.Path))
            {
                var tp = new TreeParser();
                tp.Refresh(tree);

                BabePackage.WindowManager.RefreshEditorOutline();
                BabePackage.WindowManager.RefreshOutlineWnd();
            }
            else
            {
                //文件已经被移除
                IntellisenseHelper.RemoveFile(file.Path);
                FileManager.CurrentFile = null;
            }
        }

        public static void SetCurrentFile(string file)
        {
            var tp = new TreeParser();
            tp.HandleFile(file);
            FileManager.Instance.SetActiveFile(file);
            BabePackage.WindowManager.RefreshEditorOutline();
            System.Diagnostics.Debug.Print("Current File is : " + file);
        }

        public static void RemoveFile(string file)
        {
            var curfile = FileManager.CurrentFile;
            for (int i = 0; i < FileManager.Files.Count; i++)
            {
                if (FileManager.Files[i].Path == file)
                {
                    FileManager.Files.RemoveAt(i);
                    break;
                }
            }
            if(curfile != null)
            {
                FileManager.SetActiveFile(curfile.Path);
            }
        }

        public static bool ContainsTable(string table)
        {
            for (int i = 0; i < FileManager.Files.Count; i++)
            {
                if (FileManager.Files[i].ContainsTable(table)) return true;
            }

            return InnerTables.ContainsTable(table);
        }

        public static bool ContainsFunction(string function)
        {
            for (int i = 0; i < FileManager.Files.Count; i++)
            {
                if (FileManager.Files[i].ContainsFunction(function)) return true;
            }

            return InnerTables.ContainsFunction(function);
        }

        public static LuaTable GetTable(string table)
        {
            LuaTable lt = null;
            for (int i = 0; i < FileManager.Files.Count; i++)
            {
                lt = FileManager.Files[i].GetTable(table);
                if (lt != null) return lt;
            }
            return InnerTables.GetTable(table);
        }

        public static IEnumerable<LuaMember> GetGlobal()
        {
            var list = FileManager.GetAllGlobals();
            list.AddRange(InnerTables.Members);
            return list.Distinct();
        }

        public static IEnumerable<LuaMember> GetFileTokens()
        {
            return FileManager.CurrentFileToken.Distinct();
        }
    }
}
