// **********************************************************************
//
// Copyright (c) 2009-2017 ZeroC, Inc. All rights reserved.
//
// **********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

namespace IceBuilder
{
    public class ProjectUtil
    {
        public static Guid GetProjecGuid(IVsProject project)
        {
            IVsHierarchy hierarchy = project as IVsHierarchy;
            if(hierarchy != null)
            {
                try
                {
                    Guid guid;
                    ErrorHandler.ThrowOnFailure(hierarchy.GetGuidProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectIDGuid, out guid));
                    return guid;
                }
                catch(Exception)
                {
                }
            }
            return new Guid();
        }

        //
        // Get the Guid that idenifies the type of the project
        //
        public static Guid GetProjecTypeGuid(IVsProject project)
        {
            IVsHierarchy hierarchy = project as IVsHierarchy;
            if(hierarchy != null)
            {
                try
                {
                    Guid type;
                    ErrorHandler.ThrowOnFailure(hierarchy.GetGuidProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_TypeGuid, out type));
                    return type;
                }
                catch(Exception)
                {
                }
            }
            return new Guid();
        }
        public static void SaveProject(IVsProject project)
        {
            ErrorHandler.ThrowOnFailure(Package.Instance.IVsSolution.SaveSolutionElement(
                (uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_ForceSave, project as IVsHierarchy, 0));
        }

        //
        // Get the name of a IVsHierachy item give is item id.
        //
        public static string GetItemName(IVsProject project, uint itemid)
        {
            object value;
            (project as IVsHierarchy).GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_Name, out value);
            return value == null ? string.Empty : value.ToString();
        }

        public static IVsProject GetParentProject(IVsProject project)
        {
            object value = null;
            ErrorHandler.ThrowOnFailure(((IVsHierarchy)project).GetProperty(
                VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ParentHierarchy, out value));
            return value as IVsProject;
        }

        public static List<string> GetIceBuilderItems(IVsProject project)
        {
            IVsProject4 project4 = project as IVsProject4;
            return project4 != null ? GetIceBuilderItems(project4) : GetIceBuilderItems(project as IVsHierarchy);
        }

        public static List<string> GetIceBuilderItems(IVsProject4 project)
        {
            List<string> items = new List<String>();
            uint sz = 0;
            project.GetFilesWithItemType("SliceCompile", 0, null, out sz);
            if(sz > 0)
            {
                uint[] ids = new uint[sz];
                project.GetFilesWithItemType("SliceCompile", sz, ids, out sz);
                foreach(uint id in ids)
                {
                    items.Add(GetItemName(project, id));
                }
            }
            return items;
        }
        public static List<string> GetIceBuilderItems(IVsHierarchy project)
        {
            List<string> items = new List<string>();
            GetIceBuilderItems(project, VSConstants.VSITEMID_ROOT, ref items);
            return items;
        }

        public static void GetIceBuilderItems(IVsHierarchy h, uint itemId, ref List<String> items)
        {
            IntPtr nestedValue = IntPtr.Zero;
            uint nestedId = 0;
            Guid nestedGuid = typeof(IVsHierarchy).GUID;
            int result = h.GetNestedHierarchy(itemId, ref nestedGuid, out nestedValue, out nestedId);
            if(ErrorHandler.Succeeded(result) && nestedValue != IntPtr.Zero && nestedId == VSConstants.VSITEMID_ROOT)
            {
                // Get the nested hierachy
                IVsProject project = Marshal.GetObjectForIUnknown(nestedValue) as IVsProject;
                Marshal.Release(nestedValue);
                if(project != null)
                {
                    GetIceBuilderItems(project as IVsHierarchy, VSConstants.VSITEMID_ROOT, ref items);
                }
            }
            else
            {
                // Get the first visible child node
                object value;
                result = h.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_FirstVisibleChild, out value);
                while(result == VSConstants.S_OK && value != null)
                {
                    uint child = DTEUtil.GetItemId(value);
                    if(child == VSConstants.VSITEMID_NIL)
                    {
                        // No more nodes
                        break;
                    }
                    else
                    {
                        result = h.GetProperty(child, (int)__VSHPROPID.VSHPROPID_Name, out value);
                        string path = value as string;
                        if(IsSliceFileName(path))
                        {
                            items.Add(path);
                        }
                        GetIceBuilderItems(h, child, ref items);

                        // Get the next visible sibling node
                        result = h.GetProperty(child, (int)__VSHPROPID.VSHPROPID_NextVisibleSibling, out value);
                    }
                }
            }
        }

        public static bool IsSliceFileName(string name)
        {
            return !string.IsNullOrEmpty(name) && Path.GetExtension(name).Equals(".ice");
        }

        //
        // Non DTE
        //
        public static string GetCSharpGeneratedItemPath(string sliceName)
        {
            return GetGeneratedItemPath(sliceName, ".cs");
        }

        public static string GetCppGeneratedSourceItemPath(IVsProject project, string sliceName)
        {
            return GetGeneratedItemPath(sliceName, GetEvaluatedProperty(project, PropertyNames.New.SourceExt, ".h"));
        }

        public static string GetCppGeneratedHeaderItemPath(IVsProject project, string sliceName)
        {
            return GetGeneratedItemPath(sliceName, GetEvaluatedProperty(project, PropertyNames.New.HeaderExt, ".h"));
        }

        private static string GetGeneratedItemPath(string sliceName, string extension)
        {
            return Path.GetFileName(Path.ChangeExtension(sliceName, extension));
        }

        public static string GetPathRelativeToProject(IVsProject project, string path)
        {
            return FileUtil.RelativePath(GetProjectBaseDirectory(project), path);
        }

        public static string GetProjectBaseDirectory(IVsProject project)
        {
            string fullPath;
            ErrorHandler.ThrowOnFailure(project.GetMkDocument(VSConstants.VSITEMID_ROOT, out fullPath));
            return Path.GetFullPath(Path.GetDirectoryName(fullPath));
        }

        public static string GetProjectFullPath(IVsProject project)
        {
            try
            {
                string fullPath;
                ErrorHandler.ThrowOnFailure(project.GetMkDocument(VSConstants.VSITEMID_ROOT, out fullPath));
                return Path.GetFullPath(fullPath);
            }
            catch(NotImplementedException)
            {
                return string.Empty;
            }
        }

        public static EnvDTE.ProjectItem GetProjectItem(IVsProject project, uint item)
        {
            IVsHierarchy hierarchy = project as IVsHierarchy;
            object value = null;
            if(hierarchy != null)
            {
                hierarchy.GetProperty(item, (int)__VSHPROPID.VSHPROPID_ExtObject, out value);
            }
            return value as EnvDTE.ProjectItem;
        }

        public static string GetOutputDir(IVsProject project, bool isHeader, bool evaluated)
        {
            string outputdir = null;
            if(isHeader)
            {
                outputdir = evaluated ? GetEvaluatedProperty(project, PropertyNames.New.HeaderOutputDir) :
                                        GetProperty(project, PropertyNames.New.HeaderOutputDir);
            }

            if(string.IsNullOrEmpty(outputdir))
            {
                outputdir = evaluated ? GetEvaluatedProperty(project, PropertyNames.New.OutputDir) :
                                        GetProperty(project, PropertyNames.New.OutputDir);
            }
            if(evaluated)
            {
                return Path.GetFullPath(Path.Combine(GetProjectBaseDirectory(project), outputdir));
            }
            else
            {
                return outputdir;
            }
        }

        public static string GetProperty(IVsProject project, string name)
        {
            return GetProperty(project, name, string.Empty);
        }

        public static string GetProperty(IVsProject project, string name, string defaultValue)
        {
            string value = MSBuildUtils.GetProperty(MSBuildUtils.LoadedProject(GetProjectFullPath(project), DTEUtil.IsCppProject(project), true), name);
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        public static void SetProperty(IVsProject project, string name, string value)
        {
            var fullPath = GetProjectFullPath(project);
            DTEUtil.EnsureFileIsCheckout(fullPath);
            MSBuildUtils.SetProperty(MSBuildUtils.LoadedProject(fullPath, DTEUtil.IsCppProject(project), true), "IceBuilder", name, value);
        }

        public static string GetEvaluatedProperty(IVsProject project, string name)
        {
            return GetEvaluatedProperty(project, name, string.Empty);
        }

        public static string GetEvaluatedProperty(IVsProject project, string name, string defaultValue)
        {
            string value = MSBuildUtils.GetEvaluatedProperty(MSBuildUtils.LoadedProject(GetProjectFullPath(project), DTEUtil.IsCppProject(project), true), name);
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        public static string GetProjectName(IVsProject project)
        {
            IVsProject parent = GetParentProject(project);
            if(parent != null)
            {
                return Path.Combine(GetProjectName(parent), GetItemName(project, VSConstants.VSITEMID_ROOT));
            }
            else
            {
                return GetItemName(project, VSConstants.VSITEMID_ROOT);
            }
        }

        //
        // Using DTE
        //
        public static EnvDTE.ProjectItem FindProjectItem(string path)
        {
            return Package.Instance.DTE2.Solution.FindProjectItem(path);
        }

        public static Dictionary<string, List<string>> GetGeneratedFiles(IVsProject project)
        {
            return GetGeneratedFiles(project, DTEUtil.IsIceBuilderNuGetInstalled(project));
        }

        public static Dictionary<string, List<string>> GetGeneratedFiles(IVsProject project, IceBuilderProjectType type)
        {
            return type == IceBuilderProjectType.CsharpProjectType ?
                GetCSharpGeneratedFiles(project) : GetCppGeneratedFiles(GetCppGeneratedFiles(project));
        }

        public static Dictionary<string, List<string>> GetCSharpGeneratedFiles(IVsProject project)
        {
            Dictionary<string, List<string>> generated = new Dictionary<string, List<string>>();
            string outputDir = GetOutputDir(project, false, true);
            List<string> items = GetIceBuilderItems(project);
            foreach(string item in items)
            {
                generated[item] = new List<string>(
                    new string[]
                    {
                        Path.GetFullPath(Path.Combine(outputDir, GetCSharpGeneratedItemPath(item)))
                    });
            }
            return generated;
        }

        public struct CppGeneratedFileSet
        {
            public EnvDTE.Configuration configuration;
            public string filename;
            public List<string> headers;
            public List<string> sources;
        }

        public static Dictionary<string, List<string>>
        GetCppGeneratedFiles(List<CppGeneratedFileSet> filesets)
        {
            Dictionary<string, List<string>> generated = new Dictionary<string, List<string>>();
            foreach(CppGeneratedFileSet fileset in filesets)
            {
                if(generated.ContainsKey(fileset.filename))
                {
                    generated[fileset.filename] = generated[fileset.filename].Union(fileset.headers).Union(fileset.sources).ToList();
                }
                else
                {
                    generated[fileset.filename] = fileset.headers.Union(fileset.sources).ToList();
                }
            }
            return generated;
        }

        public static IVsCfg[]
        GetProjectConfigurations(IVsProject project)
        {
            IVsCfgProvider provider = project as IVsCfgProvider;
            uint[] sz = new uint[1];
            provider.GetCfgs(0, null, sz, null);
            if(sz[0] > 0)
            {
                IVsCfg[] cfgs = new IVsCfg[sz[0]];
                provider.GetCfgs(sz[0], cfgs, sz, null);
                return cfgs;
            }
            return new IVsCfg[0];
        }

        public static List<CppGeneratedFileSet>
        GetCppGeneratedFiles(IVsProject project)
        {
            List<string> outputDirectories = new List<string>();
            List<string> headerOutputDirectories = new List<string>();

            //
            // Check if the output directories expand to different values in each configuration, if that is the case we
            // add generated files per configuration, and use ExcludeFromBuild to disable the file in all the configurations
            // but the one matching the configuration expanded value of the properties.
            //
            //
            IVsBuildPropertyStorage propertyStorage = project as IVsBuildPropertyStorage;
            IVsCfg[] configurations = GetProjectConfigurations(project);
            foreach(IVsCfg config in configurations)
            {
                string value;
                string configName;
                config.get_DisplayName(out configName);
                propertyStorage.GetPropertyValue("SliceCompileOutputDir", configName, (uint)_PersistStorageType.PST_PROJECT_FILE, out value);
                if(!string.IsNullOrEmpty(value) && !outputDirectories.Contains(value))
                {
                    outputDirectories.Add(value);
                }
                propertyStorage.GetPropertyValue("SliceCompileHeaderOutputDir", configName, (uint)_PersistStorageType.PST_PROJECT_FILE, out value);
                if(!string.IsNullOrEmpty(value) && !outputDirectories.Contains(value))
                {
                    headerOutputDirectories.Add(value);
                }
            }
            bool generateFilesPerConfiguration = headerOutputDirectories.Count > 1 || outputDirectories.Count > 1;

            List<CppGeneratedFileSet> generated = new List<CppGeneratedFileSet>();
            List<string> items = GetIceBuilderItems(project);
            if(items.Count > 0)
            {
                string projectDir = GetProjectBaseDirectory(project);

                string outputDir = GetOutputDir(project, false, false);
                string headerOutputDir = GetOutputDir(project, true, false);

                string sourceExt = GetEvaluatedProperty(project, PropertyNames.New.SourceExt, ".cpp");
                string headerExt = GetEvaluatedProperty(project, PropertyNames.New.HeaderExt, ".h");

                EnvDTE.Project p = DTEUtil.GetProject(project as IVsHierarchy);
                if(generateFilesPerConfiguration)
                {
                    foreach(EnvDTE.Configuration configuration in p.ConfigurationManager)
                    {
                        string outputDirEvaluated = Path.Combine(projectDir, Package.Instance.VCUtil.Evaluate(configuration, outputDir));
                        string headerOutputDirEvaluated = Path.Combine(projectDir, Package.Instance.VCUtil.Evaluate(configuration, headerOutputDir));

                        CppGeneratedFileSet fileset = new CppGeneratedFileSet();
                        fileset.configuration = configuration;
                        fileset.headers = new List<string>();
                        fileset.sources = new List<string>();

                        foreach(string item in items)
                        {
                            fileset.filename = item;
                            fileset.sources.Add(Path.GetFullPath(Path.Combine(outputDirEvaluated, GetGeneratedItemPath(item, sourceExt))));
                            fileset.headers.Add(Path.GetFullPath(Path.Combine(headerOutputDirEvaluated, GetGeneratedItemPath(item, headerExt))));
                        }
                        generated.Add(fileset);
                    }
                }
                else
                {
                    EnvDTE.Configuration configuration = p.ConfigurationManager.ActiveConfiguration;
                    string outputDirEvaluated = Path.Combine(projectDir, Package.Instance.VCUtil.Evaluate(configuration, outputDir));
                    string headerOutputDirEvaluated = Path.Combine(projectDir, Package.Instance.VCUtil.Evaluate(configuration, headerOutputDir));

                    CppGeneratedFileSet fileset = new CppGeneratedFileSet();
                    fileset.configuration = configuration;
                    fileset.headers = new List<string>();
                    fileset.sources = new List<string>();

                    foreach(string item in items)
                    {
                        fileset.filename = item;
                        fileset.sources.Add(Path.GetFullPath(Path.Combine(outputDirEvaluated, GetGeneratedItemPath(item, sourceExt))));
                        fileset.headers.Add(Path.GetFullPath(Path.Combine(headerOutputDirEvaluated, GetGeneratedItemPath(item, headerExt))));
                    }
                    generated.Add(fileset);
                }
            }
            return generated;
        }

        public static bool
        CheckGenerateFileIsValid(IVsProject project, IceBuilderProjectType projectType, string path)
        {
            if(projectType == IceBuilderProjectType.CsharpProjectType)
            {
                string outputDir = GetOutputDir(project, false, true);
                string generatedSource = GetCSharpGeneratedItemPath(Path.GetFileName(path));
                if(File.Exists(generatedSource))
                {
                    const string message =
                        "A file named '{0}' already exists.\nIf you want to add '{1}' first remove '{0}'.";

                    UIUtil.ShowErrorDialog("Ice Builder",
                        string.Format(message,
                            GetPathRelativeToProject(project, generatedSource),
                            GetPathRelativeToProject(project, path)));
                    return false;
                }
            }
            else if(projectType == IceBuilderProjectType.CppProjectType)
            {
                string source = GetCppGeneratedSourceItemPath(project, path);
                string header = GetCppGeneratedHeaderItemPath(project, path);

                EnvDTE.Project p = DTEUtil.GetProject(project as IVsHierarchy);

                foreach(EnvDTE.Configuration config in p.ConfigurationManager)
                {
                    string outputDir = Package.Instance.VCUtil.Evaluate(config, "$(IceBuilderOutputDir)");
                    outputDir = Path.GetFullPath(Path.Combine(GetProjectBaseDirectory(project), outputDir));
                    string headerOutputDir = Package.Instance.VCUtil.Evaluate(config, "$(IceBuilderHeaderOutputDir)");
                    if(string.IsNullOrEmpty(headerOutputDir))
                    {
                        headerOutputDir = outputDir;
                    }
                    else
                    {
                        headerOutputDir = Path.GetFullPath(Path.Combine(GetProjectBaseDirectory(project), headerOutputDir));
                    }
                    string generatedSource = Path.GetFullPath(Path.Combine(outputDir, source));
                    string generatedHeader = Path.GetFullPath(Path.Combine(headerOutputDir, header));

                    if(File.Exists(generatedSource) || File.Exists(generatedHeader))
                    {
                        const string message =
                            "A file named '{0}' or '{1}' already exists.\nIf you want to add '{2}' first remove '{0}' and '{1}'.";

                        UIUtil.ShowErrorDialog("Ice Builder",
                            string.Format(message,
                                GetPathRelativeToProject(project, generatedSource),
                                GetPathRelativeToProject(project, generatedHeader),
                                GetPathRelativeToProject(project, path)));
                        return false;
                    }
                }
            }
            return true;
        }

        public static void DeleteItems(List<string> paths)
        {
            foreach(string path in paths)
            {
                EnvDTE.ProjectItem item = FindProjectItem(path);
                if(item != null)
                {
                    item.Remove();
                }

                if(File.Exists(path))
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch(IOException)
                    {
                        // can happen if the file is being used by other process
                    }
                }
            }
        }
        public static void SetupGenerated(IVsProject project, EnvDTE.Configuration configuration, string filter, List<string> files, bool generatedFilesPerConfiguration)
        {
            List<string> missing = new List<string>();
            foreach(string file in files)
            {
                if(!Directory.Exists(Path.GetDirectoryName(file)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                }

                int found;
                uint id;
                VSDOCUMENTPRIORITY[] priority = new VSDOCUMENTPRIORITY[1];
                project.IsDocumentInProject(file, out found, priority, out id);
                if(found == 0)
                {
                    missing.Add(file);
                }
            }

            Package.Instance.VCUtil.AddGeneratedFiles(DTEUtil.GetProject(project as IVsHierarchy), configuration, filter, missing, generatedFilesPerConfiguration);
        }
        static List<string> KnownHeaderExtension = new List<string>(new string[] { ".h", ".hpp", ".hh", ".hxx" });
        public static void SetupGenerated(IVsProject project, IceBuilderProjectType type)
        {
            if(type == IceBuilderProjectType.CppProjectType)
            {
                //
                // This will ensure that property reads don't use a cached project.
                //
                MSBuildUtils.LoadedProject(ProjectUtil.GetProjectFullPath(project), true, false);

                List<CppGeneratedFileSet> generated = GetCppGeneratedFiles(project);
                foreach(CppGeneratedFileSet fileset in generated)
                {
                    SetupGenerated(project, fileset.configuration, "Source Files", fileset.sources, generated.Count > 1);
                    SetupGenerated(project, fileset.configuration, "Header Files", fileset.headers, generated.Count > 1);
                }
                Package.Instance.FileTracker.Reap(GetProjectFullPath(project), GetCppGeneratedFiles(generated));
            }
            else // C# project
            {
                EnvDTE.Project p = DTEUtil.GetProject(project as IVsHierarchy);
                Dictionary<string, List<string>> generated = GetCSharpGeneratedFiles(project);
                foreach(KeyValuePair<string, List<string>> i in generated)
                {
                    foreach(string file in i.Value)
                    {
                        if(!Directory.Exists(Path.GetDirectoryName(file)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(file));
                        }

                        EnvDTE.ProjectItem item = FindProjectItem(file);
                        if(item == null)
                        {
                            if(!File.Exists(file))
                            {
                                File.Create(file).Dispose();
                            }
                            p.ProjectItems.AddFromFile(file);
                            try
                            {
                                //
                                // Remove the file otherwise it will be considered up to date.
                                //
                                File.Delete(file);
                            }
                            catch(Exception)
                            {
                            }
                        }
                    }
                }
                Package.Instance.FileTracker.Reap(GetProjectFullPath(project), generated);
            }
        }

        public static bool AddAssemblyReference(EnvDTE.Project project, string assembliesDir, string component)
        {
            string path = Path.Combine(assembliesDir, string.Format("{0}.dll", component));
            VSLangProj.VSProject vsProject = (VSLangProj.VSProject)project.Object;
            try
            {
                VSLangProj80.Reference3 reference = (VSLangProj80.Reference3)vsProject.References.Add(path);
                TrySetCopyLocal(reference);
                TrySetSpecificVersion(reference);
                TrySetHintPath(reference);
                return true;
            }
            catch(COMException)
            {
            }
            return false;
        }

        private static void TrySetCopyLocal(VSLangProj80.Reference3 reference)
        {
            //
            // Always set copy local to true for references that we add
            //
            try
            {
                //
                // In order to properly write this to MSBuild in ALL cases, we have to trigger the Property Change
                // notification with a new value of "true". However, "true" is the default value, so in order to
                // cause a notification to fire, we have to set it to false and then back to true
                //
                reference.CopyLocal = false;
                reference.CopyLocal = true;
            }
            catch(NotSupportedException)
            {
            }
            catch(NotImplementedException)
            {
            }
            catch(COMException)
            {
            }
        }

        private static void TrySetSpecificVersion(VSLangProj80.Reference3 reference)
        {
            //
            // Allways set SpecificVersion to false so that references still work
            // when Ice Home setting is updated.
            //
            try
            {
                reference.SpecificVersion = true;
                reference.SpecificVersion = false;
            }
            catch(NotSupportedException)
            {
            }
            catch(NotImplementedException)
            {
            }
            catch(COMException)
            {
            }
        }

        public static VSLangProj.References GetProjectRererences(EnvDTE.Project project)
        {
            return ((VSLangProj.VSProject)project.Object).References;
        }

        public static void UpgradReferencesHintPath(EnvDTE.Project project)
        {
            foreach(VSLangProj80.Reference3 r in GetProjectRererences(project))
            {
                if(Package.AssemblyNames.Contains(r.Name))
                {
                    TrySetHintPath(r);
                }
            }
        }

        private static void TrySetHintPath(VSLangProj80.Reference3 reference)
        {
            try
            {
                Microsoft.Build.Evaluation.Project project =
                    MSBuildUtils.LoadedProject(reference.ContainingProject.FullName, false, false);

                Microsoft.Build.Evaluation.ProjectItem item =
                    project.AllEvaluatedItems.FirstOrDefault(i =>
                                i.ItemType.Equals("Reference") &&
                                i.EvaluatedInclude.Split(",".ToCharArray()).ElementAt(0).Equals(reference.Name)
                            );
                if(item != null)
                {
                    item.SetMetadataValue("HintPath", Path.Combine("$(IceAssembliesDir)",
                                                                   string.Format("{0}.dll", reference.Name)));
                }
            }
            catch(NotSupportedException)
            {
            }
            catch(NotImplementedException)
            {
            }
            catch(COMException)
            {
            }
        }

        public static bool RemoveAssemblyReference(EnvDTE.Project project, string component)
        {
            foreach(VSLangProj.Reference r in ((VSLangProj.VSProject)project.Object).References)
            {
                if(r.Name.Equals(component, StringComparison.OrdinalIgnoreCase))
                {
                    r.Remove();
                    return true;
                }
            }
            return false;
        }

        public static bool HasAssemblyReference(EnvDTE.Project project, string component)
        {
            foreach(VSLangProj.Reference r in ((VSLangProj.VSProject)project.Object).References)
            {
                if(r.Name.Equals(component, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static VSLangProj.Reference FindAssemblyReference(EnvDTE.Project project, String component)
        {
            foreach(VSLangProj.Reference r in ((VSLangProj.VSProject)project.Object).References)
            {
                if(r.Name.Equals(component, StringComparison.OrdinalIgnoreCase))
                {
                    return r;
                }
            }
            return null;
        }
    }
}
