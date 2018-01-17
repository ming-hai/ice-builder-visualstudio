﻿// **********************************************************************
//
// Copyright (c) 2009-2018 ZeroC, Inc. All rights reserved.
//
// **********************************************************************

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
