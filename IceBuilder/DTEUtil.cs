// **********************************************************************
//
// Copyright (c) 2009-2018 ZeroC, Inc. All rights reserved.
//
// **********************************************************************

using System;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace IceBuilder
{
    public enum IceBuilderProjectType
    {
        None,
        CppProjectType,
        CsharpProjectType
    }

    public class DTEUtil
    {
        public static uint GetItemId(object value)
        {
            if(value == null)
            {
                return VSConstants.VSITEMID_NIL;
            }
            if(value is int)
            {
                return (uint)(int)value;
            }
            if(value is uint)
            {
                return (uint)value;
            }
            if(value is short)
            {
                return (uint)(short)value;
            }
            if(value is long)
            {
                return (uint)(long)value;
            }
            return VSConstants.VSITEMID_NIL;
        }

        public static List<IVsProject> GetProjects()
        {
            IEnumHierarchies enumHierarchies;
            Guid guid = Guid.Empty;
            uint flags = (uint)__VSENUMPROJFLAGS.EPF_ALLPROJECTS;
            ErrorHandler.ThrowOnFailure(Package.Instance.IVsSolution.GetProjectEnum(flags, guid, out enumHierarchies));

            List<IVsProject> projects = new List<IVsProject>();

            IVsHierarchy[] hierarchies = new IVsHierarchy[1];
            uint sz;
            do
            {
                ErrorHandler.ThrowOnFailure(enumHierarchies.Next(1, hierarchies, out sz));
                if(sz > 0)
                {
                    var project = hierarchies[0] as IVsProject;
                    if(project != null)
                    {
                        projects.Add(project);
                    }
                }
            }
            while(sz == 1);
            return projects;
        }

        public static void GetSubProjects(IVsProject p, ref List<IVsProject> projects)
        {
            IVsHierarchy h = p as IVsHierarchy;
            // Get the first visible child node
            object value;
            int result = h.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_FirstVisibleChild, out value);
            while(ErrorHandler.Succeeded(result))
            {
                uint child = GetItemId(value);
                if(child == VSConstants.VSITEMID_NIL)
                {
                    // No more nodes
                    break;
                }
                else
                {
                    GetSubProjects(h, child, ref projects);

                    // Get the next visible sibling node
                    value = null;
                    result = h.GetProperty(child, (int)__VSHPROPID.VSHPROPID_NextVisibleSibling, out value);
                }
            }
        }

        public static void GetSubProjects(IVsHierarchy h, uint itemId, ref List<IVsProject> projects)
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
                    projects.Add(project);
                    GetSubProjects(project, ref projects);
                }
            }
        }

        public static IVsProject GetSelectedProject()
        {
            IVsHierarchy hier = null;
            var sp = new ServiceProvider(Package.Instance.DTE as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            IVsMonitorSelection selectionMonitor = sp.GetService(typeof(IVsMonitorSelection)) as IVsMonitorSelection;

            //
            // There isn't an open project.
            //
            if(selectionMonitor != null)
            {
                IntPtr ppHier;
                uint pitemid;
                IVsMultiItemSelect ppMIS;
                IntPtr ppSC;
                ErrorHandler.ThrowOnFailure(selectionMonitor.GetCurrentSelection(out ppHier, out pitemid, out ppMIS, out ppSC));

                if(ppHier != IntPtr.Zero)
                {
                    hier = (IVsHierarchy)Marshal.GetObjectForIUnknown(ppHier);
                    Marshal.Release(ppHier);
                }

                if(ppSC != IntPtr.Zero)
                {
                    Marshal.Release(ppSC);
                }
            }
            return hier as IVsProject;
        }

        public static bool IsCppProject(IVsProject project)
        {
            Guid type = ProjectUtil.GetProjecTypeGuid(project);
            return type.Equals(cppProjectGUID) || type.Equals(cppStoreAppProjectGUID);
        }

        public static bool IsCSharpProject(IVsProject p)
        {
            return ProjectUtil.GetProjecTypeGuid(p).Equals(csharpProjectGUID) &&
                MSBuildUtils.IsCSharpProject(p.GetMSBuildProject(true));
        }

        public static IceBuilderProjectType IsIceBuilderEnabled(IVsProject project)
        {
            if(project != null)
            {
                IceBuilderProjectType type = IsCppProject(project) ? IceBuilderProjectType.CppProjectType :
                                             IsCSharpProject(project) ? IceBuilderProjectType.CsharpProjectType : IceBuilderProjectType.None;
                if(type != IceBuilderProjectType.None)
                {
                    if(MSBuildUtils.IsIceBuilderEnabled(project.GetMSBuildProject(true)))
                    {
                        return type;
                    }
                }
            }
            return IceBuilderProjectType.None;
        }

        public static IceBuilderProjectType IsIceBuilderNuGetInstalled(IVsProject project)
        {
            if (project != null)
            {
                IceBuilderProjectType type = IsCppProject(project) ? IceBuilderProjectType.CppProjectType :
                                             IsCSharpProject(project) ? IceBuilderProjectType.CsharpProjectType : IceBuilderProjectType.None;
                if (type != IceBuilderProjectType.None)
                {
                    if(Package.Instance.NuGet.IsPackageInstalled(project.GetDTEProject(), Package.NuGetBuilderPackageId) ||
                       MSBuildUtils.HasIceBuilderPackageReference(project.GetMSBuildProject(true)))
                    {
                        return type;
                    }
                }
            }
            return IceBuilderProjectType.None;
        }

        public static bool EnsureFileIsCheckout(string path)
        {
            var sc = Package.Instance.DTE.SourceControl;
            if(sc != null)
            {
                if(sc.IsItemUnderSCC(path) && !sc.IsItemCheckedOut(path))
                {
                    return sc.CheckOutItem(path);
                }
            }
            return true;
        }

        public static readonly Guid cppProjectGUID =
            new Guid("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}");
        public static readonly Guid cppStoreAppProjectGUID =
            new Guid("{BC8A1FFA-BEE3-4634-8014-F334798102B3}");
        public static readonly Guid csharpProjectGUID =
            new Guid("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
        public static readonly Guid unloadedProjectGUID =
            new Guid("{67294A52-A4F0-11D2-AA88-00C04F688DDE}");
    }
}
