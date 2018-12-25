using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babe.Lua
{
    class LuaProject
    {
        const string SettingFolder = "BabeLua";
        const string ProjectFolder = "Project";
        const string LuaProjectName = "LuaProject.luaproj";
        const string LuaProjectFactoryGuid = "{5697748A-77EF-44CA-8824-4F5637E5945B}";
        public static EnvDTE.Project GetStartupProject(EnvDTE.DTE dte)
        {
            //获取当前启动调试项目（可能是右键启动新实例的项目，而非选择的启动项目）
            EnvDTE.Project activeProject = null;
            Array activeSolutionProjects = Babe.Lua.Package.BabePackage.Current.DTE.ActiveSolutionProjects as Array;
            if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
            {
                activeProject = activeSolutionProjects.GetValue(0) as EnvDTE.Project;
                if (activeProject != null && activeProject.Kind.ToUpper() == LuaProjectFactoryGuid)
                    return activeProject;
            }

            EnvDTE.SolutionBuild solutionBuild = dte.Solution.SolutionBuild;
            if (solutionBuild.StartupProjects == null)
                return null;

            //当前启动项目是LuaProject，则返回该LuaProject
            string startupItem = "";
            foreach (String item in (Array)solutionBuild.StartupProjects)
            {
                startupItem += item;
            }
            EnvDTE.Project startupProject = dte.Solution.Item(startupItem);
            if (startupProject != null && startupProject.Kind.ToUpper() == LuaProjectFactoryGuid)
                return startupProject;

            //如果启动项不是LuaProject，则返回第一个LuaProject
            return GetFirstLuaProject(dte);
            /*
            EnvDTE.Project activeProject = null;
            Array activeSolutionProjects = LuaToolsPackage.Instance.DTE.ActiveSolutionProjects as Array;
            if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
            {
                activeProject = activeSolutionProjects.GetValue(0) as EnvDTE.Project;
            }
            EnvDTE.Projects projects = LuaToolsPackage.Instance.DTE.Solution.Projects;
            foreach(EnvDTE.Project project in projects)
            {
                string name = project.Name;
                string extension = Path.GetExtension(project.FullName);
                if (extension.ToLower() != ".luaproj")
                    continue;
            }
            */
        }
        const int ProjectItemDefault = 0;
        const int ProjectItemFolder = 1;
        const int ProjectItemFile = 2;
        public static int GetProjectItemKind(EnvDTE.ProjectItem projectItem)
        {
            if (projectItem == null)
                return ProjectItemDefault;

            foreach (EnvDTE.Property property in projectItem.Properties)
            {
                if (property.Name == "FolderName")
                {
                    return ProjectItemFolder;
                }
                else if (property.Name == "FileName")
                {
                    return ProjectItemFile;
                }
            }
            return ProjectItemDefault;
        }
        public static EnvDTE.ProjectItem IsFileInProjectItems(EnvDTE.ProjectItems projectItems, string filePath)
        {
            foreach (EnvDTE.ProjectItem projectItem in projectItems)
            {
                for (short i = 0; i < projectItem.FileCount; i++)
                {
                    string fileName = projectItem.get_FileNames(i);
                    if (fileName.ToLower() == filePath.ToLower())
                    {
                        return projectItem;
                    }
                }
                if (projectItem.ProjectItems != null)
                {
                    EnvDTE.ProjectItem subProjectItem = IsFileInProjectItems(projectItem.ProjectItems, filePath);
                    if (subProjectItem != null)
                        return subProjectItem;
                }
            }
            return null;
        }
        public static EnvDTE.ProjectItem IsFileInProject(EnvDTE.Project project, string filePath)
        {
            if (project == null)
                return null;

            return IsFileInProjectItems(project.ProjectItems, filePath);
        }
        public static EnvDTE.Project GetFirstLuaProject(EnvDTE.DTE dte)
        {
            EnvDTE.Projects projects = dte.Solution.Projects;
            if (projects == null)
                return null;

            foreach(EnvDTE.Project project in projects)
            {
                if (project.Kind.ToUpper() == LuaProjectFactoryGuid)
                    return project;
            }
            return null;
        }
        public static bool AddFileLinkToFirstLuaProject(EnvDTE.DTE dte, string file)
        {
            if (!System.IO.File.Exists(file))
                return false;

            EnvDTE.Project firstLuaProject = GetFirstLuaProject(dte);
            if ((firstLuaProject != null) && (firstLuaProject.Kind.ToUpper() == LuaProjectFactoryGuid))
            {
                EnvDTE.ProjectItem projectItem = IsFileInProject(firstLuaProject, file);
                if (projectItem == null)
                {
                    firstLuaProject.ProjectItems.AddFromFile(file);
                }
                return true;
            }
            return false;
        }
        public static bool SetStartupProject(EnvDTE.DTE dte, EnvDTE.Project project)
        {
            if (project != null)
            {
                //设置为启动项目
                EnvDTE.Property property = dte.Solution.Properties.Item("StartupProject");
                if (property != null)
                {
                    property.Value = project.Name;
                    return true;
                }
            }
            return false;
        }
        public static void CloseAllTempLuaProject()
        {
            EnvDTE.DTE dte = Babe.Lua.Package.BabePackage.Current.DTE;

            EnvDTE.Projects projects = dte.Solution.Projects;
            if (projects == null)
                return;

            string projectFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingFolder, ProjectFolder);

            string solutionFullName = dte.Solution.FullName;
            foreach (EnvDTE.Project project in projects)
            {
                if (project.Kind.ToUpper() == LuaProjectFactoryGuid)
                {
                    string fullName = project.FullName;
                    if (project.FullName.Contains(projectFolder))
                    {
                        project.Save();
                        project.Delete();
                    }
                    //dte.Solution.Remove(project);
                }
            }
            if (dte.Solution.Count == 0)
            {
                dte.Solution.Close(false);
            }
            else
            {
                //dte.Solution.SaveAs(solutionFullName);
            }
        }
        public static bool AddFileLinkToCurrentLuaProjectOrCreate(EnvDTE.DTE dte, string file)
        {
            if (AddFileLinkToFirstLuaProject(dte, file))
                return true;

            EnvDTE.Solution solution = dte.Solution;
            try
            {
                string templatePath = solution.get_TemplatePath(LuaProjectFactoryGuid);
                if (string.IsNullOrEmpty(templatePath))
                    return false;

                templatePath += LuaProjectName;

                string projectName = "temp_";//System.IO.Path.GetRandomFileName();
                if (Babe.Lua.Package.BabePackage.Setting != null)
                {
                    projectName += Babe.Lua.Package.BabePackage.Setting.CurrentSetting;
                }
                string projectFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingFolder, ProjectFolder);
                string projectPath = System.IO.Path.Combine(projectFolder, projectName);

                string projectFullName = System.IO.Path.Combine(projectPath, projectName+".luaproj");
                //只在temp工程不存在时创建
                if(/*(dte.Solution.Count == 0) ||*/ !System.IO.File.Exists(projectFullName))
                {
                    //创建LuaProject
                    EnvDTE.Project project = solution.AddFromTemplate(templatePath, projectPath, projectName, false);
                }
                else
                {
                    solution.AddFromFile(projectFullName, false);
                }
            }
            catch
            {
                //System.Windows.Forms.MessageBox.Show("Lua Template Path is null,make sure BabeLuaDebug has been installed.");
            }

            return AddFileLinkToFirstLuaProject(dte, file);
        }
    }
}
