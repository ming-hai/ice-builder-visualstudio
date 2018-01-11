using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IceBuilder
{
    public delegate void NuGetBatchEnd();

    public interface NuGet
    {
        void OnNugetBatchEnd(NuGetBatchEnd batchEnd);

        bool IsPackageInstalled(EnvDTE.Project project, string packageId);
        void InstallLatestPackage(EnvDTE.Project project, string packageId);
    }
}
