using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Babe.Lua.DataModel;
using Irony.Parsing;
using Babe.Lua.Grammar;
using System.IO;
using Babe.Lua.Package;

namespace Babe.Lua.Helper
{
    class CCImporter
    {
        LuaFile File;
        public void Work()
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            dialog.DefaultExt = ".lua";

            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var file = dialog.FileName;

                try
                {
                    FileStream fileStream = System.IO.File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        var parser = new Parser(LuaGrammar.Instance);
                        var tree = parser.Parse(reader.ReadToEnd());

                        File = new LuaFile(file, null);

                        File.Members.AddRange(UpdateData(tree.Root));
                    }

                }
                catch (Exception e)
                {

                }

            }
        }

        List<LuaMember> UpdateData(ParseTreeNode node)
        {
            List<LuaMember> Members = new List<LuaMember>();

            foreach (var child in node.ChildNodes)
            {
                #region Assign
                if (child.Term.Name == LuaTerminalNames.Assign)
                {
                    var varlist = child.ChildNodes[0].ChildNodes;
                    var explist = child.ChildNodes[2].ChildNodes;
                    var length = varlist.Count > explist.Count ? explist.Count : varlist.Count;
                    for (int i = 0; i < length; i++)
                    {
                        //var =  Name | prefixexp `[´ exp `]´ | prefixexp `.´ Name
                        //explist = {exp `,´} exp
                        ParseTreeNode var = varlist[i];
                        ParseTreeNode exp = explist[i];

                        #region table.member = xxx | table[member] = xxx
                        if ((var.ChildNodes.Count == 3 || var.ChildNodes.Count == 4)
                            )//table.member = xxx || table[member] = xxx
                        {
                            var member = var.ChildNodes[2];
                            var table = var.ChildNodes[0];

                            if (table.ChildNodes[0].Term.Name == LuaTerminalNames.Var && table.ChildNodes[0].ChildNodes.Count == 1)
                            {
                                var tbname = table.ChildNodes[0].ChildNodes[0].Token.ValueString;

                                var luatable = File.GetTable(tbname);
                                if (luatable != null)
                                {
                                    Token token = null;
                                    if (member.Token == null)
                                    {
                                        if (member.ChildNodes.Count == 1 && member.ChildNodes[0].Token != null && member.ChildNodes[0].Token.EditorInfo.Type == TokenType.String)
                                            token = member.ChildNodes[0].Token;
                                    }
                                    else
                                    {
                                        token = member.Token;
                                    }
                                    if (token != null)
                                    {
                                        if (exp.ChildNodes.Count == 1 && exp.ChildNodes[0].Term.Name == LuaTerminalNames.NamelessFunction)
                                        {
                                            luatable.Members.Add(HandleNamelessFunction(token, exp.ChildNodes[0]));
                                        }
                                        else
                                        {
                                            luatable.Members.Add(new LuaMember(token.ValueString, token.Location.Line, token.Location.Column));
                                        }
                                        //AddMemToTable(tbname, mename);
                                    }
                                }
                            }
                        }
                        #endregion
                        else
                        {
                            var token = var.ChildNodes[0].Token;
                            exp = exp.ChildNodes[0];
                            Members.Add(HandleAssign(token, exp));
                        }
                    }
                }
                #endregion
                #region function XXX(..) || local function XXX(...)
                else if (child.Term.Name == LuaTerminalNames.NamedFunction)
                {
                    HandleNamedFunction(child);
                }
                else if (child.Term.Name == LuaTerminalNames.LocalVar)
                {
                    if (child.ChildNodes[1].Term.Name == LuaTerminalNames.NamedFunction)
                    {
                        HandleNamedFunction(child.ChildNodes[1]);
                    }
                    else
                    {
                        var namelist = child.ChildNodes[1].ChildNodes;
                        var explist = child.ChildNodes[2].ChildNodes[0].ChildNodes;
                        if (explist.Count == 2)
                        {
                            explist = explist[1].ChildNodes;
                            var length = namelist.Count > explist.Count ? explist.Count : namelist.Count;

                            for (int i = 0; i < length; i++)
                            {
                                var name = namelist[i];
                                var exp = explist[i];
                                Members.Add(HandleAssign(name.Token, exp));
                            }
                        }
                    }
                }
                #endregion
                else
                {
                    Members.AddRange(UpdateData(child));
                }
            }

            return Members;
        }

        LuaMember HandleAssign(Token token, ParseTreeNode exp)
        {
            if (exp.Term.Name == LuaTerminalNames.NamelessFunction)// name = function(...)
            {
                return new LuaFunction(File, token.ValueString, token.Location.Line, GetFunctionArgs(exp.ChildNodes[1]));
            }
            else if (exp.Term.Name == LuaTerminalNames.TableConstructor)// name = {...}
            {
                return HandleTableConstructor(token, exp);
            }

            return new LuaMember(token);
        }

        LuaTable HandleTableConstructor(Token token, ParseTreeNode node)
        {
            var table = new LuaTable(File, token.ValueString, token.Location.Line);
            var fieldlist = node.ChildNodes[1].ChildNodes[0];
            if (fieldlist.ChildNodes != null && fieldlist.ChildNodes.Count > 0)
            {
                fieldlist = fieldlist.ChildNodes[0];
                var fields = new List<ParseTreeNode>();
                FilterChildren(fieldlist, LuaTerminalNames.Field, ref fields, 2);
                foreach (var field in fields)
                {
                    var exp = field.ChildNodes.Last().ChildNodes[0];
                    if (field.ChildNodes.Count == 3)// name = xxx
                    {
                        if (exp.Term.Name == LuaTerminalNames.NamelessFunction)
                        {
                            table.Members.Add(HandleNamelessFunction(field.ChildNodes[0].Token, exp));
                        }
                        else table.Members.Add(new LuaMember(field.ChildNodes[0].Token));
                    }
                    else if (field.ChildNodes.Count == 5)//[expr] = xxx
                    {
                        var expr = field.ChildNodes[1];
                        if (expr.ChildNodes.Count == 1 && expr.ChildNodes[0].Term.Name == LuaTerminalNames.String)
                        {
                            if (exp.Term.Name == LuaTerminalNames.NamelessFunction)
                            {
                                table.Members.Add(HandleNamelessFunction(expr.ChildNodes[0].Token, exp));
                            }
                            else
                            {
                                var lm = new LuaMember(expr.ChildNodes[0].Token);
                                var comment = new StringBuilder("--[Comment]");
                                comment.AppendLine();
                                comment.Append(exp.Token.ValueString);
                                lm.Comment = comment.ToString();
                                table.Members.Add(lm);
                            }
                        }
                    }
                }
            }
            return table;
        }

        /// <summary>
        /// 从node中筛选名字为name的子元素，存放在result数组中，最多遍历level层。如果level为0，则遍历全部子元素。
        /// </summary>
        void FilterChildren(ParseTreeNode node, string name, ref List<ParseTreeNode> result, int level = 0)
        {
            if (level == 0) return;
            level -= 1;

            List<ParseTreeNode> fields = new List<ParseTreeNode>();

            foreach (var child in node.ChildNodes)
            {
                if (child.Term.Name == name) result.Add(child);
                FilterChildren(child, name, ref result, level);
            }



            return;
        }

        LuaFunction HandleNamedFunction(ParseTreeNode namedfunc)
        {
            LuaFunction function = null;

            var funcname = namedfunc.ChildNodes[1];

            var name1 = funcname.ChildNodes[0].Token;
            var part2 = funcname.ChildNodes[1];
            var part3 = funcname.ChildNodes[2].ChildNodes[0];

            var args = GetFunctionArgs(namedfunc.ChildNodes[2]);

            if (part2.ChildNodes.Count == 0 && part3.ChildNodes.Count == 0)
            {
                function = new LuaFunction(File, name1.ValueString, name1.Location.Line, args);
            }
            else if (part2.ChildNodes.Count == 1 && part3.ChildNodes.Count == 0)
            {
                if (part2.ChildNodes[0].ChildNodes.Count == 2)
                {
                    //AddMemToTable(name1, part2.ChildNodes[0].ChildNodes[1].Token.ValueString);
                    var table = File.GetTable(name1.ValueString);
                    if (table != null) table.Members.Add(new LuaFunction(part2.ChildNodes[0].ChildNodes[1].Token.ValueString, name1.Location.Line, args));
                }
            }
            else if (part2.ChildNodes.Count == 0 && part3.ChildNodes.Count == 2)
            {
                //AddMemToTable(name1, part3.ChildNodes[1].Token.ValueString);
                var table = File.GetTable(name1.ValueString);
                if (table != null) table.Members.Add(new LuaFunction(part3.ChildNodes[1].Token.ValueString, name1.Location.Line, args));
            }
            return function;
        }

        LuaFunction HandleNamelessFunction(Token name, ParseTreeNode node)
        {
            var function = new LuaFunction(name.ValueString, name.Location.Line, GetFunctionArgs(node.ChildNodes[1]));

            return function;
        }

        string[] GetFunctionArgs(ParseTreeNode funcBody)
        {
            List<string> args = new List<string>();
            var parlist = funcBody.ChildNodes[1].ChildNodes[0].ChildNodes;
            if (parlist != null && parlist.Count > 0)
            {
                var list = parlist[0].ChildNodes[0].ChildNodes;
                if (list != null && list.Count > 0)
                {
                    foreach (var ide in list)
                    {
                        args.Add(ide.Token.ValueString);
                    }
                }
            }
            return args.ToArray();
        }
    }
}
