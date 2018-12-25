using Babe.Lua;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Babe.Lua.Grammar;
using Babe.Lua.Package;

namespace Babe.Lua.DataModel
{
	class FileManager
	{
		public List<LuaFile> Files { get; private set; }
        
		public List<LuaMember> CurrentFileToken { get; private set; }

		int _current = -1;
		public LuaFile CurrentFile
		{
			get
			{
				if (_current != -1) return Files[_current];
				else return null;
			}
			set
			{
				if (_current != -1 && value != null)
				{
					Files[_current] = value;
					RefreshCurrentFile();
				}
				else
				{
					_current = -1;
				}
			}
		}

		static FileManager _instance;
		public static FileManager Instance
		{
			get
			{
				if (_instance == null) _instance = new FileManager();
				return _instance;
			}
		}

		private FileManager()
		{
			CurrentFileToken = new List<LuaMember>();
			Files = new List<LuaFile>();
		}

		public void ClearData()
		{
			Files.Clear();
			CurrentFileToken.Clear();
			_current = -1;
		}

		public void AddFile(LuaFile file)
		{
			var index = Files.IndexOf(file);
			if (index != -1) Files[index] = file;
			else
			{
				Files.Add(file);
			}
		}

		public void SetActiveFile(string file)
		{
			for (int i = 0; i < Files.Count; i++)
			{
				if (file.Equals(Files[i].Path))
				{
					_current = i;

					RefreshCurrentFile();

					return;
				}
			}

            _current = -1;
		}

		public void RefreshCurrentFile()
		{
			if (CurrentFile != null)
			{
				CurrentFileToken = new List<LuaMember>();
				foreach (var token in CurrentFile.Tokens)
				{
					if (token.Category == Irony.Parsing.TokenCategory.Content && token.Terminal.Name == LuaTerminalNames.Identifier)
						CurrentFileToken.Add(new LuaMember(CurrentFile, token));
				}
			}
		}

		public List<LuaMember> GetAllGlobals()
		{
			List<LuaMember> list = new List<LuaMember>();
			for (int i = 0; i < Files.Count; i++)
			{
				list.AddRange(Files[i].Members);
			}
			return list;
		}

        #region Search Methods
        public List<LuaMember> SearchInFile(LuaFile file, string keyword, bool CaseSensitive)
		{
			List<LuaMember> members = new List<LuaMember>();

			if (file != null)
			{
				try
				{
                    var options = System.Text.RegularExpressions.RegexOptions.None;
                    if (!CaseSensitive) options = System.Text.RegularExpressions.RegexOptions.IgnoreCase;
					System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(System.Text.RegularExpressions.Regex.Escape(keyword), options);
					
					List<string> lines = new List<string>();
					using (StreamReader sr = new StreamReader(file.Path))
					{
						while (sr.Peek() >= 0)
						{
							lines.Add(sr.ReadLine());
						}
					}

					for (int i = 0; i < lines.Count; i++)
					{
						var match = reg.Match(lines[i]);
						while (match.Success)
						{
							var lm = new LuaMember(file, keyword, i, match.Index);
							lm.Preview = lines[i];
							members.Add(lm);
							match = match.NextMatch();
						}
					}

					//int index = -1;
					//for(int i = 0;i<lines.Count;i++)
					//{
					//	index = lines[i].IndexOf(keyword);
					//	while (index != -1)
					//	{
					//		var lm = new LuaMember(keyword, i, index);
					//		lm.Preview = lines[i];
					//		lm.File = file.File;
					//		members.Add(lm);
					//		index = lines[i].IndexOf(keyword, index + keyword.Length);
					//	}
					//}
				}
				catch { }
			}

			return members;
		}

		public List<LuaMember> Search(string keyword, bool AllFile, bool CaseSensitive)
		{
			List<LuaMember> results = new List<LuaMember>();
			if (!AllFile)
			{
				//var result = FindWordTaggerProvider.CurrentTagger.SearchText(keyword);
				//foreach (var span in result)
				//{
				//	var line = span.Start.GetContainingLine();
				//	var lm = new LuaMember(keyword, line.LineNumber, span.Start - line.Start);
				//	lm.File = BabePackage.DTEHelper.DTE.ActiveDocument.FullName;
				//	lm.Preview = line.GetText();
				//	results.Add(lm);
				//}
				BabePackage.DTEHelper.DTE.ActiveDocument.Save();
				results = SearchInFile(CurrentFile, keyword, CaseSensitive);
			}
			else
			{
				BabePackage.DTEHelper.DTE.Documents.SaveAll();
				//bool saved = false;
				//while (!saved)
				//{
				//	saved = true;
				//	foreach (EnvDTE.Document doc in BabePackage.DTEHelper.DTE.Documents)
				//	{
				//		if (!doc.Saved)
				//		{
				//			saved = false;
				//			break;
				//		}
				//	}
				//}

				//List<LuaMember> members = new List<LuaMember>();

				//HashSet<string> OpenFiles = new HashSet<string>();
				//foreach (EnvDTE.Document doc in BabePackage.DTEHelper.DTE.Documents)
				//{
				//	OpenFiles.Add(doc.FullName);

					
				//}

				for (int i = 0; i < Files.Count; i++)
				{
					//if (OpenFiles.Contains(Files[i].File)) continue;
					results.AddRange(SearchInFile(Files[i], keyword, CaseSensitive));
				}
			}

			return results;
		}
        #endregion

        #region Find Definition Methods
        //public List<LuaMember> FindDefination(string keyword)
        //{
        //    List<LuaMember> members = new List<LuaMember>();

        //    string[] keywords = keyword.Split(new char[] { '.', ':' }, StringSplitOptions.RemoveEmptyEntries);
        //    if (keywords.Length == 0) return null;

        //    for (int i = 0; i < Files.Count; i++)
        //    {
        //        try
        //        {
        //            List<string> lines = new List<string>();
        //            using (StreamReader sr = new StreamReader(Files[i].File))
        //            {
        //                while (sr.Peek() >= 0)
        //                {
        //                    lines.Add(sr.ReadLine());
        //                }
        //            }

        //            foreach (LuaMember member in Files[i].Members)
        //            {
        //                if (member.Name.Equals(keyword))
        //                {
        //                    var lmp = member.Copy();
        //                    lmp.Preview = lines[member.Line];
        //                    lmp.File = Files[i];
        //                    members.Add(lmp);
        //                }
        //            }
        //        }
        //        catch { }
        //    }

        //    return members;
        //}

        public LuaMember FindDefination(string keyword)
        {
            LuaMember result = null;

            string[] keywords = keyword.Split(new char[] { '.', ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (keywords.Length != 0)
            {
                foreach(LuaFile file in Files)
                {
                    if(TryFindDefinition(file, keywords, out result))
                    {
                        break;
                    }
                }
            }
            return result;
        }

        bool TryFindDefinition(LuaFile file, string[] keyword, out LuaMember result)
        {
            result = null;
            foreach(var lm in file.Members)
            {
                if (TryMatch(lm, keyword, 0, out result))
                {
                    return true;
                }
            }
            return false;
        }

        bool TryMatch(LuaMember tree, string[] keyword, int index, out LuaMember result)
        {
            result = null;

            LuaMember _cur = tree;

            if(_cur.Name.Equals(keyword[index]))
            {
                if(index == keyword.Length - 1)
                {
                    result = _cur;
                    return true;
                }
                else if(_cur is LuaTable)
                {
                    foreach(var lm in (_cur as LuaTable).Members)
                    {
                        if(TryMatch(lm, keyword, index + 1, out result))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }

            return false;
        }
        #endregion

        #region Find References Methods
        public IEnumerable<LuaMember> FindReferencesInFile(LuaFile file, string keyword)
		{
			if (file != null)
			{
				List<string> lines = new List<string>();
				try
				{
					using (StreamReader sr = new StreamReader(file.Path))
					{
						while (sr.Peek() >= 0)
						{
							lines.Add(sr.ReadLine());
						}
					}
				}
				catch 
                {
                    yield break;
                }

                foreach (var token in file.Tokens)
                {
                    if (token.EditorInfo == null || token.EditorInfo.Type != Irony.Parsing.TokenType.String)
                    {
                        if (token.ValueString.Equals(keyword) && lines[token.Location.Line].Contains(keyword))
                        {
                            var lmp = new LuaMember(file, token);
                            lmp.Preview = lines[lmp.Line];
                            yield return lmp;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(token.ValueString)) continue;
                        int index = token.ValueString.IndexOf(keyword);
                        if (index != -1)
                        {

                            var lmp = new LuaMember(file, keyword, token.Location.Line, token.Location.Column + 1 + index);
                            lmp.Preview = lines[lmp.Line];
                            yield return lmp;
                        }
                    }
                }
			}
		}

		public IEnumerable<IEnumerable<LuaMember>> FindReferences(string keyword, bool AllFile)
		{
			if (!AllFile)
			{
				BabePackage.DTEHelper.DTE.ActiveDocument.Save();
				yield return FindReferencesInFile(CurrentFile, keyword);
			}
			else
			{
				BabePackage.DTEHelper.DTE.Documents.SaveAll();

				for (int i = 0; i < Files.Count; i++)
				{
					yield return FindReferencesInFile(Files[i], keyword);
                }
			}
        }
        #endregion
    }
}
