// **********************************************************************
//
// Copyright (c) 2009-2017 ZeroC, Inc. All rights reserved.
//
// **********************************************************************

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace IceBuilder
{

    [Guid(PropertyPageGUID)]
    public class PropertyPage : IPropertyPage2, IPropertyPage, IDisposable
    {
        public const string PropertyPageGUID = "1E2800FE-37C5-4FD3-BC2E-969342EE08AF";

        private CSharpConfigurationView _view;
        public CSharpConfigurationView ConfigurationView
        {
            get;
            set;
        }

        public PropertyPage()
        {
            ConfigurationView = new CSharpConfigurationView(this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(ConfigurationView != null)
            {
                ConfigurationView.Dispose();
                ConfigurationView = null;
            }
        }

        #region IPropertyPage2 methods

        public void Activate(IntPtr parentHandle, RECT[] pRect, int modal)
        {
            try
            {
                RECT rect = pRect[0];
                ConfigurationView.Initialize(Control.FromHandle(parentHandle),
                                             Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom));
            }
            catch(Exception ex)
            {
                Package.UnexpectedExceptionWarning(ex);
                throw;
            }
        }

        public void Apply()
        {
            try
            {
                Settings.OutputDir = ConfigurationView.OutputDir;
                Settings.IncludeDirectories = string.Join(";", ConfigurationView.IncludeDirectories.Values);
                Settings.AdditionalOptions = ConfigurationView.AdditionalOptions;
                Settings.Save();
                ConfigurationView.Dirty = false;
            }
            catch(Exception ex)
            {
                Package.UnexpectedExceptionWarning(ex);
                throw;
            }
        }

        public void Deactivate()
        {
            try
            {
                if(_view != null)
                {
                    _view.Dispose();
                    _view = null;
                }
            }
            catch(Exception ex)
            {
                Package.UnexpectedExceptionWarning(ex);
                throw;
            }
        }

        public void EditProperty(int DISPID)
        {
        }

        public void GetPageInfo(PROPPAGEINFO[] pageInfo)
        {
            try
            {
                PROPPAGEINFO proppageinfo;
                proppageinfo.cb = (uint)Marshal.SizeOf(typeof(PROPPAGEINFO));
                proppageinfo.dwHelpContext = 0;
                proppageinfo.pszDocString = null;
                proppageinfo.pszHelpFile = null;
                proppageinfo.pszTitle = "Slice Compile";
                proppageinfo.SIZE.cx = ConfigurationView.Size.Width;
                proppageinfo.SIZE.cy = ConfigurationView.Size.Height;
                pageInfo[0] = proppageinfo;
            }
            catch(Exception ex)
            {
                Package.UnexpectedExceptionWarning(ex);
                throw;
            }
        }

        public void Help(string pszHelpDir)
        {
        }

        public int IsPageDirty()
        {
            try
            {
                return ConfigurationView.Dirty ? VSConstants.S_OK : VSConstants.S_FALSE;
            }
            catch(Exception ex)
            {
                Package.UnexpectedExceptionWarning(ex);
                throw;
            }
        }

        public void Move(RECT[] pRect)
        {
            try
            {
                Rectangle rect = Rectangle.FromLTRB(pRect[0].left, pRect[0].top, pRect[0].right, pRect[0].bottom);
                ConfigurationView.Location = new Point(rect.X, rect.Y);
                ConfigurationView.Size = new Size(rect.Width, rect.Height);
            }
            catch(Exception ex)
            {
                Package.UnexpectedExceptionWarning(ex);
                throw;
            }
        }

        public ProjectSettigns Settings
        {
            get;
            private set;
        }

        public IVsProject Project
        {
            get;
            private set;
        }

        public void SetObjects(uint cObjects, object[] objects)
        {
            try
            {
                if(objects != null && cObjects > 0)
                {
                    IVsBrowseObject browse = objects[0] as IVsBrowseObject;
                    if(browse != null)
                    {
                        IVsHierarchy hier;
                        uint id;
                        browse.GetProjectItem(out hier, out id);
                        Project = hier as IVsProject;
                        if(Project != null)
                        {
                            Settings = new ProjectSettigns(Package.Instance.ProjectManagerFactory.GetProjectManager(Project));
                            Settings.Load();
                            ConfigurationView.LoadSettigns(Settings);
                            Settings.ProjectManager.ProjectChanged += (sender, args) =>
                                {
                                    if(!ConfigurationView.Dirty)
                                    {
                                        Settings.Load();
                                        ConfigurationView.LoadSettigns(Settings);
                                    }
                                };
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Package.UnexpectedExceptionWarning(ex);
                throw;
            }
        }

        public IPropertyPageSite PageSite
        {
            get;
            private set;
        }

        public void SetPageSite(IPropertyPageSite site)
        {
            PageSite = site;
        }

        public const int SW_SHOW = 5;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_HIDE = 0;

        public void Show(uint show)
        {
            switch(show)
            {
                case SW_HIDE:
                    {
                        ConfigurationView.Hide();
                        break;
                    }
                case SW_SHOW:
                case SW_SHOWNORMAL:
                    {
                        ConfigurationView.Show();
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        public int TranslateAccelerator(MSG[] pMsg)
        {
            try
            {
                Message message = Message.Create(pMsg[0].hwnd, (int)pMsg[0].message, pMsg[0].wParam, pMsg[0].lParam);
                int hr = ConfigurationView.ProcessAccelerator(ref message);
                pMsg[0].lParam = message.LParam;
                pMsg[0].wParam = message.WParam;
                return hr;
            }
            catch(Exception ex)
            {
                Package.UnexpectedExceptionWarning(ex);
                throw;
            }
        }

        #endregion

        #region IPropertyPage methods
        int IPropertyPage.Apply()
        {
            Apply();
            return VSConstants.S_OK;
        }
        #endregion
    }
}
