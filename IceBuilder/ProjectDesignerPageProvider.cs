
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Properties;
using System;

namespace IceBuilder
{
    class SliceCompilePageMetadata : IPageMetadata
    {
        string IPageMetadata.Name => "Slice Compile";

        Guid IPageMetadata.PageGuid => new Guid(PropertyPage.PropertyPageGUID);

        int IPageMetadata.PageOrder => 3;

        bool IPageMetadata.HasConfigurationCondition => false;
    }

    [Export(typeof(IVsProjectDesignerPageProvider))]
    [AppliesTo("SliceCompile")]
    internal class ProjectDesignerPageProvider : IVsProjectDesignerPageProvider
    {
        public Task<IReadOnlyCollection<IPageMetadata>> GetPagesAsync()
        {
            return Task.FromResult((IReadOnlyCollection<IPageMetadata>)ImmutableArray.Create(SliceCompile));
        }

        private static SliceCompilePageMetadata SliceCompile = new SliceCompilePageMetadata();
    }
}
