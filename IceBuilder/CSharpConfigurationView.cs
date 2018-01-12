// **********************************************************************
//
// Copyright (c) 2009-2017 ZeroC, Inc. All rights reserved.
//
// **********************************************************************

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;

namespace IceBuilder
{
    public partial class CSharpConfigurationView : UserControl
    {
        private PropertyPage Page
        {
            get;
            set;
        }

        public CSharpConfigurationView(PropertyPage page)
        {
            Page = page;
            InitializeComponent();
            includeDirectories.PropertyPage = Page;
        }

        public void LoadSettigns(ProjectSettigns settings)
        {
            OutputDir = settings.OutputDir;
            IncludeDirectories.Values = new List<string>(
                settings.IncludeDirectories.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            AdditionalOptions = settings.AdditionalOptions;
            Dirty = false;
        }

        public virtual void Initialize(Control parent, Rectangle rect)
        {
            SetBounds(rect.X, rect.Y, rect.Width, rect.Height);
            Parent = parent;
        }

        public int ProcessAccelerator(ref Message keyboardMessage)
        {
            if(FromHandle(keyboardMessage.HWnd).PreProcessMessage(ref keyboardMessage))
            {
                return VSConstants.S_OK;
            }
            return VSConstants.S_FALSE;
        }

        public readonly uint PageStatusDirty = 0x1;
        public readonly uint PageStatusClean = 0x4;
        public bool _dirty;
        public bool Dirty
        {
            get
            {
                return _dirty;
            }
            set
            {
                _dirty = value;
                if(Page.PageSite != null)
                {
                    Page.PageSite.OnStatusChange(value ? PageStatusDirty : PageStatusClean);
                }
            }
        }

        public string OutputDir
        {
            get
            {
                return txtOutputDir.Text;
            }
            set
            {
                txtOutputDir.Text = value;
            }
        }

        public string AdditionalOptions
        {
            get
            {
                return txtAdditionalOptions.Text;
            }
            set
            {
                txtAdditionalOptions.Text = value;
            }
        }

        public IncludeDirectories IncludeDirectories
        {
            get
            {
                return includeDirectories;
            }
        }

        private void btnOutputDirectoryBrowse_Click(object sender, EventArgs e)
        {
            includeDirectories.EndEditing(true);
            string projectDir = Path.GetFullPath(Path.GetDirectoryName(ProjectUtil.GetProjectFullPath(Page.Project)));
            string selectedPath = UIUtil.BrowserFolderDialog(Handle, "Output Directory", projectDir);
            if(!string.IsNullOrEmpty(selectedPath))
            {
                selectedPath = FileUtil.RelativePath(projectDir, selectedPath);
                OutputDir = string.IsNullOrEmpty(selectedPath) ? "." : selectedPath;
                if(!txtOutputDir.Text.Equals(Page.Settings.OutputDir))
                {
                    Dirty = true;
                }
            }
        }

        private void OutputDirectory_Leave(object sender, EventArgs e)
        {
            if(!txtOutputDir.Text.Equals(Page.Settings.OutputDir))
            {
                Dirty = true;
            }
        }

        private void AdditionalOptions_Leave(object sender, EventArgs e)
        {
            if(!txtAdditionalOptions.Text.Equals(Page.Settings.AdditionalOptions))
            {
                Dirty = true;
            }
        }

        private void txtOutputDir_TextChanged(object sender, EventArgs e)
        {
            if(!txtOutputDir.Text.Equals(Page.Settings.OutputDir))
            {
                Dirty = true;
            }
        }

        private void OutputDirectory_Enter(object sender, EventArgs e)
        {
            includeDirectories.EndEditing(true);
        }

        private void AdditionalOptions_Enter(object sender, EventArgs e)
        {
            includeDirectories.EndEditing(true);
        }
    }
}
