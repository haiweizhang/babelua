using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Babe.Lua.DataModel;
using Babe.Lua.Editor;
using Babe.Lua.Package;

namespace Babe.Lua.ToolWindows
{
    public partial class SearchToolControl : UserControl
    {
        SolidColorBrush brush1 = new SolidColorBrush(Color.FromArgb(32,0,0,0));
        SolidColorBrush brush2 = new SolidColorBrush(Color.FromArgb(0,255,255,255));

        public SearchToolControl()
        {
            InitializeComponent();
            current = brush1;

            ListView.DragEnter += (s, e) => { };

            Button_RelativePath.IsChecked = BabePackage.Setting.SearchResultRelativePath;
        }

        Brush current;
        Brush GetNextBrush()
        {
            if (current == brush2)
            {
                current = brush1;
            }
            else
            {
                current = brush2;
            }
            return current;
        }

        internal void Refresh(IEnumerable<LuaMember> list)
        {
            var set = Babe.Lua.Package.BabePackage.Current.CurrentSetting;
            var pathbase = string.Empty;
            if (set != null) pathbase = set.Folder;

            ListView.Items.Clear();
            int i = 0;
            var brush = GetNextBrush();

            string curFilePath = "";
            foreach (var item in list)
            {
                if (curFilePath != item.File.Path)
                {
                    brush = GetNextBrush();
                    curFilePath = item.File.Path;
                }

                var ltim = new SearchListItem(item, (++i).ToString().PadRight(4), pathbase);
                ltim.Background = brush;
                ListView.Items.Add(ltim);
            }
        }

        internal int Refresh(IEnumerable<IEnumerable<LuaMember>> list)
        {
            var set = Babe.Lua.Package.BabePackage.Current.CurrentSetting;
            var pathbase = string.Empty;
            if (set != null) pathbase = set.Folder;

            ListView.Items.Clear();
            int i = 0;
            var brush = GetNextBrush();

            string curFilePath = "";
            foreach (var item in list)
            {
                foreach (var member in item)
                {
                    if (curFilePath != member.File.Path)
                    {
                        brush = GetNextBrush();
                        curFilePath = member.File.Path;
                    }

                    var ltim = new SearchListItem(member, (++i).ToString().PadRight(4), pathbase);
                    ltim.Background = brush;
                    ListView.Items.Add(ltim);
                }
            }

            return i;
        }

		private void Search()
		{
			var txt = TextBox_SearchWord.Text;
			if (string.IsNullOrWhiteSpace(txt)) return;

			//if (!txt.Any(ch => { return ch.IsWord(); })) return;

			//if (BabePackage.Setting.ContainsSearchFilter(txt)) return;

            //fixme:here should use setting item.
            bool caseSensitive = true;

            BabePackage.WindowManager.RefreshSearchWnd(txt, true, caseSensitive, false);
		}

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListView.SelectedItem != null)
            {
                var item = (SearchListItem)(ListView.SelectedItem);
				EditorManager.OpenDocument(item.token.File.Path);
				EditorManager.GoTo(item.token.File.Path, item.token.Line, item.token.Column, item.token.Name.Length, true);
/*                var state = Keyboard.GetKeyStates(Key.LeftCtrl);
                if (state.HasFlag(KeyStates.Down))
                {
                }*/
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListView.SelectedItem != null)
            {
                //定位到选择的位置
                var item = (SearchListItem)(ListView.SelectedItem);

				EditorManager.GoTo(item.token.File.Path, item.token.Line, item.token.Column, item.token.Name.Length, true);
            }
        }

		private void Button_ClearResult_Click(object sender, RoutedEventArgs e)
		{
			ListView.Items.Clear();
            BabePackage.WindowManager.RefreshSearchWnd("", false, true);
		}

		private void Button_Search_Click(object sender, RoutedEventArgs e)
		{
			Search();
		}

        private void Button_SearchSelect_Click(object sender, RoutedEventArgs e)
        { 
            //fixme:here should use setting item.
            bool caseSensitive = true;
            EditorManager.SearchSelect(true, false, caseSensitive);
        }

		private void Button_CopyAllResult_Click(object sender, RoutedEventArgs e)
		{
			var results = new StringBuilder();
			foreach (var item in ListView.Items)
			{
				results.AppendLine(item.ToString());
			}

			Clipboard.SetText(results.ToString());
		}

		private void TextBox_SearchWord_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				Search();
			}
		}

        private void Button_RelativePath_Click(object sender, RoutedEventArgs e)
        {
            BabePackage.Setting.SearchResultRelativePath = Button_RelativePath.IsChecked.Value;
            BabePackage.Setting.Save();
        }
    }

    
}