# Ice Builder for Visual Studio

The Ice Builder for Visual Studio is a Visual Studio extension that allows you to configure the [Ice Builder for MSBuild](https://github.com/zeroc-ice/ice-builder-msbuild) for your C++ and C# projects, all within the Visual Studio IDE. It serves as a front-end for the Ice Builder for MSBuild: all the build-time processing is performed by the Ice Builder for MSBuild.

The Ice Builder for MSBuild provides support for compiling Slice source files (`.ice` files) within C++ and C# projects created by Visual Studio. It compiles these Slice files using the Slice to C++ compiler (`slice2cpp`) or the Slice to C# compiler (`slice2cs`) provided by your Ice installation.

The Ice Builder for Visual Studio is compatible with Visual Studio 2012, 2013, 2015 and 2017. It creates projects for the following types of Ice installation:
 * Ice NuGet package for Ice 3.7 or greater
 * Ice Web installation or MSI installation for Ice 3.6

## Contents

WIP

## Installation

Ice Builder is available as a Visual Studio extension in the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=ZeroCInc.IceBuilder).

If you build Ice Builder from source, simply double-click on `IceBuilder.vsix` to install the extension into Visual Studio.

## Overview

With the Ice Builder, you can add one or more Slice (`.ice`) files to a Visual Studio project. The Ice Builder for MSBuild then compiles these files by launching `slice2cpp` (for C++ projects) or `slice2cs` (for C# projects). All the Slice files in a given project are compiled through a single Slice compiler invocation.

The Ice Builder for MSBuild compiles and recompiles a Slice file as needed:
- when the generated C++ or C# source files are missing or are older than this Slice file
- when the generated C++ or C# source files are older than a Slice file included directly or indirectly by this Slice file

Ice Builder checks whether Slice files need to be compiled or recompiled each time Visual Studio loads a project, and each time you build a project. And when you remove or rename a Slice file, Ice Builder automatically removes the corresponding generated files.

## Ice Home Configuration

With Ice 3.7 or greater, you add an Ice NuGet package to your project and Ice Builder then uses this Ice installation. You don't need to specify further the Ice installation that Ice Builder uses.

With Ice 3.6, you need to specify the Ice installation used by Ice Builder with the `Tools` > `Options` > `Project and Solutions` > `Ice Builder` options page.

![Ice home screenshot](https://github.com/zeroc-ice/ice-builder-visualstudio/raw/master/Screenshots/vs2015-options.png)

:warning: This Ice Home setting is ignored when a project uses an Ice NuGet package. Installing an Ice NuGet package into a project automatically configures the project to use the Ice SDK provided by that NuGet package.

## Automatic Build Configuration

You can configure when Slice files are compiled on the `Tools` > `Options` > `Project and Solutions` > `Ice Builder` options page. 

If the automatic build option is selected, Slice files are compiled each time they are saved, otherwise they are compiled only during project builds.

## C++ Usage

### Adding Slice Files to a C++ Project

Follow these steps:

1. Add the Ice Builder NuGet package (`zeroc.icebuilder.msbuild`) to your C++ project.

   Adding the Ice Builder creates a `Slice Files` filter in your project.

2. Add one or more Slice (`.ice`) files to your project. While these Slice files can be anywhere, you may want to select a customary location such as the project's home directory or a sub-directory named `slice`.

3. Review the Ice Builder configuration of your project, as described in the section below.

### Ice Builder Configuration for a C++ Project

The Ice Builder adds an `Ice Builder` property page to the `Common Properties` of your C++ project:

![Missing cpp property page](https://github.com/zeroc-ice/ice-builder-visualstudio/raw/master/Screenshots/cpp-property-page.png)

These properties are the same for all configurations and platforms, and allow you to specify the options passed to `slice2cpp` when compiling the project's Slice files.

| Property                                | Corresponding MSBuild Property               |
| --------------------------------------- | -------------------------------------------- |
| Output Directory                        | SliceCompileOutputDir                        |
| Header Output Directory                 | SliceCompileHeaderOutputDir                  |
| Include Directories                     | SliceCompileIncludeDirectories               |
| Base Directory For Generated #include   | SliceCompileBaseDirectoryForGeneratedInclude |
| Generated Header Extension              | SliceCompileHeaderExt                        | 
| Generated Source Extension              | SliceCompileSourceExt                        | 
| Additional Options                      | SliceCompileAdditionalOptions                |

See [Customizing the Slice to C++ Compilation](https://github.com/zeroc-ice/ice-builder-msbuild/blob/master/README.md#customizing-the-slice-to-c-compilation) for a detailed description of these properties.

## C# Usage

### Adding Slice Files to a C# Project

Follow these steps:

1. Add the Ice Builder NuGet package (`zeroc.icebuilder.msbuild`) to your C# project. 

   Adding the Ice Builder creates a `Slice Files` filter in your project.
   
2. Reload your project if it targets .NET Framework.

3. Add one or more Slice (`.ice`) files to your project. While these Slice files can be anywhere, you may want to select a customary location such as the project's home directory or a sub-directory named `slice`.

4. Review the Ice Builder configuration of your project, as described in the section below.

### Ice Builder Configuration for a C# Project

The Ice Builder adds an `Ice Builder` tab to the properties of your C# project:

![Missing csharp property page](https://github.com/zeroc-ice/ice-builder-visualstudio/raw/master/Screenshots/csharp-property-page.png)

These properties are the same for all configurations and platforms, and allow you to specify the [options] passed to `slice2cs` when compiling the project's Slice files.

| Property                                |  Corresponding MSBuild Property |
| --------------------------------------- | --------------------------------|
| Output directory                        | SliceCompileOutputDir           |
| Include directories                     | SliceCompileIncludeDirectories  |
| Additional options                      | SliceCompileAdditionalOptions   | 

See [Customizing the Slice to C# Compilation](https://github.com/zeroc-ice/ice-builder-msbuild/blob/master/README.md#customizing-the-slice-to-c-compilation-1) for a detailed description of these properties.

## Building Ice Builder from Source

### Build Requirements

You need Visual Studio 2017

**AND**

to install ALL of the following Visual Studio SDKs:
- [Visual Studio 2012 SDK](https://www.microsoft.com/en-us/download/details.aspx?id=30668)
- [Visual Studio 2013 SDK](https://www.microsoft.com/en-us/download/details.aspx?id=40758)
- [Visual Studio 2015 SDK](https://msdn.microsoft.com/en-us/library/bb166441.aspx)
- [Visual Studio 2017 SDK](https://docs.microsoft.com/en-us/visualstudio/extensibility/installing-the-visual-studio-sdk)

### Build Instructions

Open the `IceBuilder.sln` solution file in Visual Studio 2017.

After building the Ice Builder extension, the VSIX package will be placed in the build output directory:
`IceBuilder\bin\Debug\IceBuilder.vsix` for debug builds, and `IceBuilder\bin\Release\IceBuilder.vsix`
for release builds.

You can sign your extension with Authenticode by setting the environment variable `SIGN_CERTIFICATE` to
the path of your PFX certificate store, and the `SIGN_PASSWORD` environment variable to the password
used by your certificate store.
