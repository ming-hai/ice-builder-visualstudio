using NuGet.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace IceBuilder
{
    class NuGetI : NuGet
    {
#if VS2017
        IVsPackageInstallerProjectEvents PackageInstallerProjectEvents
        {
            get;
            set;
        }
#else
        IVsPackageInstallerEvents PackageInstallerEvents
        {
            get;
            set;
        }

#endif
        IVsPackageInstallerServices PackageInstallerServices
        {
            get;
            set;
        }

#if VS2017
        IVsPackageInstaller2 PackageInstaller
        {
            get;
            set;
        }
#else
        IVsPackageInstaller PackageInstaller
        {
            get;
            set;
        }
#endif
        NuGetBatchEnd BatchEnd
        {
            get;
            set;
        }

        public NuGetI()
        {
            var model = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;
            PackageInstallerServices = model.GetService<IVsPackageInstallerServices>();
#if VS2017
            PackageInstallerProjectEvents = model.GetService<IVsPackageInstallerProjectEvents>();
            PackageInstallerProjectEvents.BatchEnd += PackageInstallerProjectEvents_BatchEnd;
            PackageInstaller = model.GetService<IVsPackageInstaller2>();
#else
            PackageInstaller = model.GetService<IVsPackageInstaller>();
            PackageInstallerEvents = model.GetService<IVsPackageInstallerEvents>();
            PackageInstallerEvents.PackageInstalled += PackageInstallerEvents_PackageInstalled;
#endif
        }

        public bool IsPackageInstalled(EnvDTE.Project project, string packageId)
        {
            return PackageInstallerServices.IsPackageInstalled(project, packageId);
        }

        public void InstallLatestPackage(EnvDTE.Project project, string packageId)
        {
#if VS2017
            PackageInstaller.InstallLatestPackage(null, project, packageId, false, false);
#else
            PackageInstaller.InstallPackage(null, project, packageId, "5.0.0", false);
#endif
        }

#if VS2017
        private void PackageInstallerProjectEvents_BatchEnd(IVsPackageProjectMetadata metadata)
        {
            if (BatchEnd != null)
            {
                BatchEnd();
            }
        }
#else
        private void PackageInstallerEvents_PackageInstalled(IVsPackageMetadata metadata)
        {
            if (BatchEnd != null)
            {
                BatchEnd();
            }
        }
#endif
        void NuGet.OnNugetBatchEnd(NuGetBatchEnd batchEnd)
        {
            BatchEnd = batchEnd;
        }
    }
}
