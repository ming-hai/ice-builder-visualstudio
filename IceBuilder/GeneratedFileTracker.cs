// **********************************************************************
//
// Copyright (c) 2009-2018 ZeroC, Inc. All rights reserved.
//
// **********************************************************************

using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell.Interop;

namespace IceBuilder
{
    public class GeneratedFileTracker
    {
        public void Add(IVsProject project, IceBuilderProjectType type)
        {
            string projectFullPath = ProjectUtil.GetProjectFullPath(project);
            Remove(projectFullPath);
            _generated.Add(projectFullPath,
                type == IceBuilderProjectType.CsharpProjectType ?
                    ProjectUtil.GetGeneratedFiles(ProjectUtil.GetCsharpGeneratedFiles(project)) :
                    ProjectUtil.GetGeneratedFiles(ProjectUtil.GetCppGeneratedFiles(project)));
        }

        public void Remove(string project)
        {
            if(_generated.ContainsKey(project))
            {
                _generated.Remove(project);
            }
        }

        public void Reap(string project, Dictionary<string, List<string>> newGenerated)
        {

            if(_generated.ContainsKey(project))
            {
                Dictionary<string, List<string>> oldGenerated = _generated[project];
                foreach(KeyValuePair<string, List<string>> i in oldGenerated)
                {
                    if(!newGenerated.ContainsKey(i.Key))
                    {
                        ProjectUtil.DeleteItems(i.Value);
                    }
                    else
                    {
                        var newFiles = newGenerated[i.Key];
                        var oldFiles = i.Value;
                        if(oldFiles.Except(newFiles).Any() || newFiles.Except(oldFiles).Any())
                        {
                            ProjectUtil.DeleteItems(oldFiles);
                        }
                    }
                }
            }
            _generated[project] = newGenerated;
        }

        public bool Contains(string project, string path)
        {
            Dictionary<string, List<string>> names;
            if(_generated.TryGetValue(project, out names))
            {
                foreach(KeyValuePair<string, List<string>> k in names)
                {
                    if(k.Value.Contains(path))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool Contains(string project)
        {
            return _generated.ContainsKey(project);
        }

        public void Clear()
        {
            _generated.Clear();
        }

        private Dictionary<string, Dictionary<string, List<string>>> _generated =
            new Dictionary<string, Dictionary<string, List<string>>>();
    }

}
