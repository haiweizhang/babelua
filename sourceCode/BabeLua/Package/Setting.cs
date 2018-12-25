using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Xml.Linq;
using Babe.Lua.Editor;

namespace Babe.Lua.Package
{
	public static class SettingConstants
	{
        public const string Version = "W.1.5.7.0";

		public const string SettingFolder = "BabeLua";
        public const string CompletionFolder = "Completion";

		public const string SettingFile = "Setting.xml";
		public const string KeywordsFile = "KeyWords.xml";
		public const string UserKeywordsFile = "UserKeyWords.xml";
		public const string GuidFile = "Guid";

        public const string CompletionExampleFile = "example.lua";
        public const string CompletionLua51File = "lua5.1.lua";

		public const string ErrorLogFile = "ErrorLog.txt";

		public static class SettingKeys
		{
			public const string KeyBinding = "KeyBinding";
			public const string KeyBindFolder = "FolderExplorer";
			public const string KeyBindOutline = "Outline";
			public const string KeyBindEditorOutlineLeft = "OutlineMarginLeft";
			public const string KeyBindEditorOutlineRight = "OutlineMarginRight";
			public const string KeyBindRunExec = "RunLuaExe";

			public const string SearchFilter = "SearchFilters";

			public const string UISetting = "UISettings";
			public const string HideVSView = "HideUselessView";
            public const string SearchResultRelativePath = "SearchResultRelativePath";
			public const string AllowLog = "AllowLog";

			public const string Highlight = "Highlight";
			public const string Table = "Table";
			public const string Function = "Function";

			public const string ActiveOpendFile = "Active";
			public const string ActiveOpendFileLine = "Line";
			public const string ActiveOpendFileColumn = "Column";
			public const string OpendFile = "File";

			public const string LuaSetting = "LuaSettings";
			public const string CurrentSet = "CurrentSet";
			public const string Set = "Set";
			public const string SetName = "Name";
			public const string LuaFolder = "Folder";
			public const string LuaExec = "LuaExecutable";
			public const string WorkingPath = "WorkingPath";
			public const string LuaExecArg = "CommandLine";
			public const string FileEncoding = "FileEncoding";

			public const string Keywords = "Keywords";
			public const string ClassDefinition = "ClassDefinition";
			public const string ClassConstructor = "ClassConstructor";
		}

		public static class KeywordsKeys
		{
			public const string C = "C";
			public const string Lua = "LuaFramework";
			public const string R = "r";
			public const string G = "g";
			public const string B = "b";
			public const string User = "User";
		}

		public static class Default
		{
			public const int TableR = 200;
			public const int TableG = 100;
			public const int TableB = 0;
			public const int FuncR = 200;
			public const int FuncG = 100;
			public const int FuncB = 0;
		}
	}

	class Setting
	{
		static Setting _instance;
		public static Setting Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new Setting();
				}

				return _instance;
			}
		}

		public KeyWordSettings KeyWords;

		XElement XMLOpenFiles;
		XElement XMLLuaSettings;
		XElement XMLKeyBinding;
		XElement XMLSearchFilters;
		XElement XMLUISettings;

		public Dictionary<string, LuaSet> LuaSettings { get; private set; }

        /// <summary>
        /// 是否初次安装插件
        /// </summary>
		public bool IsFirstInstall { get; set; }
		public string UserGUID { get; private set; }

		public bool HideUselessViews
		{
			get
			{
				var Element = XMLUISettings.Element(SettingConstants.SettingKeys.HideVSView);
				if (Element == null) return false;
				return Element.Value == "1" ? true : false;
			}
			set
			{
				var Element = XMLUISettings.Element(SettingConstants.SettingKeys.HideVSView);
				if (Element == null) XMLUISettings.Add(new XElement(SettingConstants.SettingKeys.HideVSView, Convert.ToInt32(value)));
				else Element.Value = Convert.ToInt32(value).ToString();
			}
		}

        public bool SearchResultRelativePath
        {
            get
            {
                var Element = XMLUISettings.Element(SettingConstants.SettingKeys.SearchResultRelativePath);
                if (Element == null) return false;
                return Element.Value == "1" ? true : false;
            }
            set
            {
                var Element = XMLUISettings.Element(SettingConstants.SettingKeys.SearchResultRelativePath);
                if (Element == null) XMLUISettings.Add(new XElement(SettingConstants.SettingKeys.SearchResultRelativePath, Convert.ToInt32(value)));
                else Element.Value = Convert.ToInt32(value).ToString();

                BabePackage.WindowManager.SetSearchWndRelativePathEnable(value);
            }
        }

		public bool AllowDebugLog
		{
			get
			{
				var Element = XMLUISettings.Element(SettingConstants.SettingKeys.AllowLog);
				if (Element == null) return false;
				return Element.Value == "1" ? true : false;
			}
			set
			{
				var Element = XMLUISettings.Element(SettingConstants.SettingKeys.AllowLog);
				if (Element == null) XMLUISettings.Add(new XElement(SettingConstants.SettingKeys.AllowLog, Convert.ToInt32(value)));
				else Element.Value = Convert.ToInt32(value).ToString();

                Boyaa.LuaDebug.SetWriteLog(Convert.ToInt32(value));
			}
		}

        public event EventHandler<bool> AllowDebugLogChanged;
        public void OnAllDebugLogChanged()
        {
            if (AllowDebugLogChanged != null)
            {
                AllowDebugLogChanged(this, AllowDebugLog);
            }
        }

		public string ClassDefinition { get; private set; }
		public string ClassConstructor { get; private set; }

		XDocument Doc;
		string FileName;

		private Setting()
		{
			string dic = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingConstants.SettingFolder);
			FileName = Path.Combine(dic, SettingConstants.SettingFile);

			if (!Directory.Exists(dic))
			{
				IsFirstInstall = true;
				Directory.CreateDirectory(dic);
			}
			CreateSettings(dic);

			KeyWords = KeyWordSettings.Instance;

			if (File.Exists(FileName))
			{
				Doc = XDocument.Load(FileName);

				//OpenFiles = Doc.Root.Element("OpenFile");
				//if (OpenFiles == null) OpenFiles = new XElement("OpenFile");

				XMLLuaSettings = Doc.Root.Element(SettingConstants.SettingKeys.LuaSetting);
				if (XMLLuaSettings == null)
				{
					XMLLuaSettings = new XElement(SettingConstants.SettingKeys.LuaSetting);
					Doc.Root.Add(XMLLuaSettings);
				}
				InitLuaSetting();

				XMLKeyBinding = Doc.Root.Element(SettingConstants.SettingKeys.KeyBinding);
				if (XMLKeyBinding == null)
				{
					XMLKeyBinding = new XElement(SettingConstants.SettingKeys.KeyBinding);
					Doc.Root.Add(XMLKeyBinding);
				}

				XMLSearchFilters = Doc.Root.Element(SettingConstants.SettingKeys.SearchFilter);
				if (XMLSearchFilters == null)
				{
					XMLSearchFilters = new XElement(SettingConstants.SettingKeys.SearchFilter);
					Doc.Root.Add(XMLSearchFilters);
				}

				XMLUISettings = Doc.Root.Element(SettingConstants.SettingKeys.UISetting);
				if (XMLUISettings == null)
				{
					XMLUISettings = new XElement(SettingConstants.SettingKeys.UISetting);
					Doc.Root.Add(XMLUISettings);
				}

				#region Init Keywords
				ClassDefinition = "class";
				ClassConstructor = "new";
				var XMLKeywords = Doc.Root.Element(SettingConstants.SettingKeys.Keywords);
				if (XMLKeywords == null)
				{
					XMLKeywords = new XElement(SettingConstants.SettingKeys.Keywords);
					XMLKeywords.Add(new XElement(SettingConstants.SettingKeys.ClassDefinition, ClassDefinition));
					XMLKeywords.Add(new XElement(SettingConstants.SettingKeys.ClassConstructor, ClassConstructor));
					Doc.Root.Add(XMLKeywords);
				}
				else
				{
					var def = XMLKeywords.Element(SettingConstants.SettingKeys.ClassDefinition);
					if (def != null) ClassDefinition = def.Value;
					var ctor = XMLKeywords.Element(SettingConstants.SettingKeys.ClassConstructor);
					if (ctor != null) ClassConstructor = ctor.Value;
				}
				#endregion

				#region InitHighlight
				XElement XMLHighlight = Doc.Root.Element(SettingConstants.SettingKeys.Highlight);
				if(XMLHighlight == null)
				{
					XMLHighlight = new XElement(SettingConstants.SettingKeys.Highlight);
					XMLHighlight.Add
					(
						new XElement
						(
							SettingConstants.SettingKeys.Function,
							new XAttribute(SettingConstants.KeywordsKeys.R, SettingConstants.Default.FuncR),
							new XAttribute(SettingConstants.KeywordsKeys.G, SettingConstants.Default.FuncG),
							new XAttribute(SettingConstants.KeywordsKeys.B, SettingConstants.Default.FuncB)
						)
					);
					KeyWords.Function = Color.FromRgb(SettingConstants.Default.FuncR, SettingConstants.Default.FuncG, SettingConstants.Default.FuncB);
					XMLHighlight.Add
					(
						new XElement
						(
							SettingConstants.SettingKeys.Table,
							new XAttribute(SettingConstants.KeywordsKeys.R, SettingConstants.Default.TableR),
							new XAttribute(SettingConstants.KeywordsKeys.G, SettingConstants.Default.TableG),
							new XAttribute(SettingConstants.KeywordsKeys.B, SettingConstants.Default.TableB)
						)
					);
					KeyWords.Table = Color.FromRgb(SettingConstants.Default.TableR, SettingConstants.Default.TableG, SettingConstants.Default.TableB);
					Doc.Root.Add(XMLHighlight);
				}
				else
				{
					var table = XMLHighlight.Element(SettingConstants.SettingKeys.Table);
					if (table != null)
					{
						var color = Color.FromRgb(
						byte.Parse(table.Attribute(SettingConstants.KeywordsKeys.R).Value),
						byte.Parse(table.Attribute(SettingConstants.KeywordsKeys.G).Value),
						byte.Parse(table.Attribute(SettingConstants.KeywordsKeys.B).Value));
						if (color.R != 0 || color.B != 0 || color.G != 0)
						{
							KeyWords.Table = color;
						}
						//为了兼容之前的版本，修改默认值
						else
						{
							table.ReplaceAttributes(
								new XAttribute(SettingConstants.KeywordsKeys.R, SettingConstants.Default.TableR),
								new XAttribute(SettingConstants.KeywordsKeys.G, SettingConstants.Default.TableG),
								new XAttribute(SettingConstants.KeywordsKeys.B, SettingConstants.Default.TableB)
								);
							KeyWords.Table = Color.FromRgb(SettingConstants.Default.TableR, SettingConstants.Default.TableG, SettingConstants.Default.TableB);
						}
					}
					else
					{
						XMLHighlight.Add
						(
							new XElement
							(
								SettingConstants.SettingKeys.Table,
								new XAttribute(SettingConstants.KeywordsKeys.R, SettingConstants.Default.TableR),
								new XAttribute(SettingConstants.KeywordsKeys.G, SettingConstants.Default.TableG),
								new XAttribute(SettingConstants.KeywordsKeys.B, SettingConstants.Default.TableB)
							)
						);
						KeyWords.Table = Color.FromRgb(SettingConstants.Default.TableR, SettingConstants.Default.TableG, SettingConstants.Default.TableB);
					}
					var function = XMLHighlight.Element(SettingConstants.SettingKeys.Function);
					if (function != null)
					{
						var color = Color.FromRgb(
						byte.Parse(function.Attribute(SettingConstants.KeywordsKeys.R).Value),
						byte.Parse(function.Attribute(SettingConstants.KeywordsKeys.G).Value),
						byte.Parse(function.Attribute(SettingConstants.KeywordsKeys.B).Value));
						if (color.R != 0 || color.B != 0 || color.G != 0)
						{
							KeyWords.Function = color;
						}
						//为了兼容之前的版本，修改默认值
						else
						{
							function.ReplaceAttributes(
								new XAttribute(SettingConstants.KeywordsKeys.R, SettingConstants.Default.FuncR),
								new XAttribute(SettingConstants.KeywordsKeys.G, SettingConstants.Default.FuncG),
								new XAttribute(SettingConstants.KeywordsKeys.B, SettingConstants.Default.FuncB)
								);
							KeyWords.Function = Color.FromRgb(SettingConstants.Default.FuncR, SettingConstants.Default.FuncG, SettingConstants.Default.FuncB);
						}
					}
					else
					{
						XMLHighlight.Add
						(
							new XElement
							(
								SettingConstants.SettingKeys.Function,
								new XAttribute(SettingConstants.KeywordsKeys.R, SettingConstants.Default.FuncR),
								new XAttribute(SettingConstants.KeywordsKeys.G, SettingConstants.Default.FuncG),
								new XAttribute(SettingConstants.KeywordsKeys.B, SettingConstants.Default.FuncB)
							)
						);
						KeyWords.Function = Color.FromRgb(SettingConstants.Default.FuncR, SettingConstants.Default.FuncG, SettingConstants.Default.FuncB);
					}
				}
				#endregion

				Save();
			}

			InitSearchFilterList();
		}

		void CreateSettings(string folder)
		{
			var path = Path.Combine(folder, SettingConstants.SettingFile);
			if (!File.Exists(path))
			{
				using (var stream = File.CreateText(path))
				{
					stream.Write(Properties.Resources.Setting);
				}
			}

			path = Path.Combine(folder, SettingConstants.KeywordsFile);
			if (!File.Exists(path))
			{
				using (var stream = File.CreateText(path))
				{
					stream.Write(Properties.Resources.KeyWords);
				}
			}

			path = Path.Combine(folder, SettingConstants.UserKeywordsFile);
			if (!File.Exists(path))
			{
				using (var stream = File.CreateText(path))
				{
					stream.Write(Properties.Resources.UserKeyWords);
				}
			}

			path = Path.Combine(folder, SettingConstants.GuidFile);
			if (!File.Exists(path))
			{
				UserGUID = Guid.NewGuid().ToString();

				using (var stream = File.CreateText(path))
				{
					stream.Write(UserGUID);
				}
			}
			else
			{
				using (var stream = new StreamReader(path))
				{
					UserGUID = stream.ReadToEnd();
				}
			}

            var comp = Path.Combine(folder, SettingConstants.CompletionFolder);
            if (!Directory.Exists(comp))
            {
                Directory.CreateDirectory(comp);
                path = Path.Combine(comp, SettingConstants.CompletionExampleFile);

                using(var stream = File.CreateText(path))
                {
                    var dat = Properties.Resources.CompletionExample;
                    stream.BaseStream.Write(dat, 0, dat.Length);
                }

                path = Path.Combine(comp, SettingConstants.CompletionLua51File);

                using (var stream = File.CreateText(path))
                {
                    var dat = Properties.Resources.CompletionLua51;
                    stream.BaseStream.Write(dat, 0, dat.Length);
                }
            }
		}

		public void Save()
		{
			Doc.Save(FileName);
		}

		#region OpenFiles
		public List<string> GetOpenFiles(ref string ActiveFile, ref int Line, ref int Column)
		{
			List<string> Files = new List<string>();

			var ActElement = XMLOpenFiles.Element(SettingConstants.SettingKeys.ActiveOpendFile);
			if (ActElement != null)
			{
				ActiveFile = ActElement.Value;
				int.TryParse(ActElement.Attribute(SettingConstants.SettingKeys.ActiveOpendFileLine).Value, out Line);
				int.TryParse(ActElement.Attribute(SettingConstants.SettingKeys.ActiveOpendFileColumn).Value, out Column);
			}

			var Elements = XMLOpenFiles.Elements(SettingConstants.SettingKeys.OpendFile);

			foreach (XElement xl in Elements)
			{
				Files.Add(xl.Value);
			}

			return Files;
		}
		#endregion

		#region Filters
		void InitSearchFilterList()
		{
			SearchFilterList = new HashSet<string>();
			foreach (XElement xe in XMLSearchFilters.Elements())
			{
				SearchFilterList.Add(xe.Name.LocalName);
			}
		}

		HashSet<string> SearchFilterList;
		public bool ContainsSearchFilter(string name)
		{
			return SearchFilterList.Contains(name);
		}

		public void SetSearchFilters(string xml)
		{
			try
			{
				XMLSearchFilters = XElement.Parse(xml);
				Doc.Root.ReplaceWith(XMLSearchFilters);
				InitSearchFilterList();
			}
			catch { }
		}
		#endregion

		#region LuaSetting
		void InitLuaSetting()
		{
            if (XMLLuaSettings.Element(SettingConstants.SettingKeys.CurrentSet) == null)
            {
                XMLLuaSettings.Add(new XElement(SettingConstants.SettingKeys.CurrentSet));
            }

			LuaSettings = new Dictionary<string, LuaSet>();

			if (XMLLuaSettings.Elements().Count() > 1 && XMLLuaSettings.Elements(SettingConstants.SettingKeys.Set).Count() == 0)
			{
				var node = XMLLuaSettings.FirstNode as XElement;
				while (node.NextNode != null)
				{
					node = node.NextNode as XElement;
					node.Add(new XElement(SettingConstants.SettingKeys.SetName, node.Name.LocalName));
					node.Name = SettingConstants.SettingKeys.Set;
				}
				Save();
			}

			List<XElement> invalids = new List<XElement>();
			foreach (XElement element in XMLLuaSettings.Elements(SettingConstants.SettingKeys.Set))
			{
				var fe = element.Element(SettingConstants.SettingKeys.FileEncoding);
				var encoding = EncodingName.UTF8;
				if (fe == null)
				{
					element.Add(new XElement(SettingConstants.SettingKeys.FileEncoding, encoding));
				}
				else
				{
					encoding = (EncodingName)Enum.Parse(typeof(EncodingName), fe.Value);
				}

				var wp = string.Empty;
				var workingpath = element.Element(SettingConstants.SettingKeys.WorkingPath);
				if (workingpath == null)
				{
					element.Add(new XElement(SettingConstants.SettingKeys.WorkingPath, ""));
				}
				else
				{
					wp = workingpath.Value;
				}

				LuaSet set = new LuaSet(
					element.Element(SettingConstants.SettingKeys.LuaFolder).Value,
					element.Element(SettingConstants.SettingKeys.LuaExec).Value,
					wp,
					element.Element(SettingConstants.SettingKeys.LuaExecArg).Value,
					encoding
					);

				if (Directory.Exists(set.Folder))
				{
					LuaSettings.Add(element.Element(SettingConstants.SettingKeys.SetName).Value, set);
				}
				else
				{
					invalids.Add(element);
				}
			}

			foreach (XElement element in invalids)
			{
				element.Remove();
			}
			Save();
		}

		public LuaSet GetSetting(string name)
		{
			if (LuaSettings.ContainsKey(name)) return LuaSettings[name];
			else return null;
		}

		public string CurrentSetting
		{
			get
			{
				return XMLLuaSettings.Element(SettingConstants.SettingKeys.CurrentSet).Value;
			}
			set
			{
				XMLLuaSettings.Element(SettingConstants.SettingKeys.CurrentSet).Value = value;
			}
		}

		public void AddSetting(string Name, string Folder, string LuaExecutable, string WorkingPath, string CommandLine, EncodingName Encoding)
		{
			var set = new LuaSet(Folder, LuaExecutable, WorkingPath, CommandLine, Encoding);
			if (LuaSettings.ContainsKey(Name))
			{
				LuaSettings[Name] = set;
			}
			else
			{
				LuaSettings.Add(Name, set);
			}


			XElement element = null;

			foreach (var xl in XMLLuaSettings.Elements(SettingConstants.SettingKeys.Set))
			{
				if (xl.Element(SettingConstants.SettingKeys.SetName).Value.Contains(Name))
				{
					element = xl;
					break;
				}
			}

			if (element == null)
			{
				element = new XElement(SettingConstants.SettingKeys.Set);
				element.Add(new XElement(SettingConstants.SettingKeys.SetName, Name));
				element.Add(new XElement(SettingConstants.SettingKeys.LuaFolder, Folder));
				element.Add(new XElement(SettingConstants.SettingKeys.LuaExec, LuaExecutable));
				element.Add(new XElement(SettingConstants.SettingKeys.WorkingPath, WorkingPath));
				element.Add(new XElement(SettingConstants.SettingKeys.LuaExecArg, CommandLine));
				element.Add(new XElement(SettingConstants.SettingKeys.FileEncoding, Encoding));
				XMLLuaSettings.Add(element);
			}
			else
			{
				element.ReplaceNodes(
					new XElement(SettingConstants.SettingKeys.SetName, Name),
					new XElement(SettingConstants.SettingKeys.LuaFolder, Folder),
					new XElement(SettingConstants.SettingKeys.LuaExec, LuaExecutable),
					new XElement(SettingConstants.SettingKeys.WorkingPath, WorkingPath),
					new XElement(SettingConstants.SettingKeys.LuaExecArg, CommandLine),
					new XElement(SettingConstants.SettingKeys.FileEncoding, Encoding)
					);
			}
		}

		public void RemoveSetting(string name)
		{
			XElement element = null;

			foreach (var xl in XMLLuaSettings.Elements(SettingConstants.SettingKeys.Set))
			{
				if (xl.Element(SettingConstants.SettingKeys.SetName).Value.Contains(name))
				{
					element = xl;
					break;
				}
			}

			if (element != null)
			{
				element.Remove();
			}

			if (LuaSettings.ContainsKey(name))
			{
				LuaSettings.Remove(name);
			}
		}

		public bool ContainsSetting(string name)
		{
			return LuaSettings.ContainsKey(name);
		}

		public IEnumerable<string> AllSetting
		{
			get
			{
				return LuaSettings.Keys.ToArray();
			}
		}

		public string GetFirstSettingName()
		{
			string firstName = "";

			if (LuaSettings.Count > 0) firstName = LuaSettings.Keys.First();

			return firstName;
		}
		#endregion

		#region KeyBinding
		public string GetKeyBindingName(string name)
		{
			var set = XMLKeyBinding.Element(name);
			if (set == null)
			{
				System.Collections.ArrayList names = new System.Collections.ArrayList() { };
				if (name == SettingConstants.SettingKeys.KeyBindFolder)
				{
					return GetKeyName(1);
				}
				else if (name == SettingConstants.SettingKeys.KeyBindOutline)
				{
					return GetKeyName(2);
				}
				else if (name == SettingConstants.SettingKeys.KeyBindEditorOutlineRight)
				{
					return GetKeyName(3);
				}
				else if (name == SettingConstants.SettingKeys.KeyBindRunExec)
				{
					return GetKeyName(4);
				}
				return string.Empty;
			}
			else
			{
				KeyBindingSet keySet = new KeyBindingSet(set.Element("Key").Value);
				return keySet.key;
			}
		}
		public void SetBindingKey(string name, string key)
		{
			XElement element = XMLKeyBinding.Element(name);
			if (element == null)
			{
				element = new XElement(name);
				element.Add(new XElement("Key", key));
				XMLKeyBinding.Add(element);
			}
			else
			{
				element.ReplaceNodes(new XElement("Key", key));
			}
		}
		public void RemoveBindingKey(string name)
		{
			var set = XMLKeyBinding.Element(name);
			if (set != null)
			{
				set.Remove();
			}
		}
		public string GetKeyName(int num)
		{
			string name = string.Format("Ctrl+{0}", num);
			return name;
		}
		public IEnumerable<string> AllBindingKey
		{
			get
			{
				List<string> names = new List<string>();

				int i;
				for (i = 1; i <= 4; i++)
				{
					names.Add(GetKeyName(i));
				}

				return names;
			}
		}
		#endregion
	}

	class KeyWordSettings
	{

		public KeyValuePair<Color, HashSet<string>> C { get; private set; }
		public KeyValuePair<Color, HashSet<string>> Framework { get; private set; }

		public List<KeyValuePair<Color, HashSet<string>>> User { get; private set; }

		public Color Table { get; set; }
		public Color Function { get; set; }

		static KeyWordSettings _instance;
		public static KeyWordSettings Instance
		{
			get
			{
				if (_instance == null) _instance = new KeyWordSettings();
				return _instance;
			}
		}

		private KeyWordSettings()
		{
			string dic = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingConstants.SettingFolder);

			var KeywordsFile = Path.Combine(dic, SettingConstants.KeywordsFile);

			if (File.Exists(KeywordsFile))
			{
				var Doc = XDocument.Load(KeywordsFile);
				var Ce = Doc.Root.Element(SettingConstants.KeywordsKeys.C);
				var Lua = Doc.Root.Element(SettingConstants.KeywordsKeys.Lua);

				var color = Color.FromRgb(byte.Parse(Ce.Attribute(SettingConstants.KeywordsKeys.R).Value), byte.Parse(Ce.Attribute(SettingConstants.KeywordsKeys.G).Value), byte.Parse(Ce.Attribute(SettingConstants.KeywordsKeys.B).Value));
				var list = new HashSet<string>();
				foreach (XElement xe in Ce.Elements())
				{
					list.Add(xe.Value);
				}
				C = new KeyValuePair<Color, HashSet<string>>(color, list);

				color = Color.FromRgb(byte.Parse(Lua.Attribute(SettingConstants.KeywordsKeys.R).Value), byte.Parse(Lua.Attribute(SettingConstants.KeywordsKeys.G).Value), byte.Parse(Lua.Attribute(SettingConstants.KeywordsKeys.B).Value));
				list = new HashSet<string>();
				foreach (XElement xe in Lua.Elements())
				{
					list.Add(xe.Value);
				}
				Framework = new KeyValuePair<Color, HashSet<string>>(color, list);
			}

			var UserKeywordsFile = Path.Combine(dic, SettingConstants.UserKeywordsFile);
			if (File.Exists(UserKeywordsFile))
			{
				var Doc = XDocument.Load(UserKeywordsFile);
				var Users = Doc.Root.Elements(SettingConstants.KeywordsKeys.User);

				User = new List<KeyValuePair<Color, HashSet<string>>>(Users.Count());

				foreach (XElement element in Users)
				{
					var color = Color.FromRgb(
						byte.Parse(element.Attribute(SettingConstants.KeywordsKeys.R).Value),
						byte.Parse(element.Attribute(SettingConstants.KeywordsKeys.G).Value),
						byte.Parse(element.Attribute(SettingConstants.KeywordsKeys.B).Value));
					var list = new HashSet<string>();
					foreach (XElement xe in element.Elements())
					{
						if (!string.IsNullOrWhiteSpace(xe.Value))
							list.Add(xe.Value);
					}
					User.Add(new KeyValuePair<Color, HashSet<string>>(color, list));
				}
			}
		}
	}
}
