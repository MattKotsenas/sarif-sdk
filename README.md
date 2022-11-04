# sarif-sdk
[![Build Status](https://dev.azure.com/mseng/1ES/_apis/build/status/microsoft.sarif-sdk?branchName=master)](https://dev.azure.com/mseng/1ES/_build/latest?definitionId=9978&branchName=main)

The SARIF SDK contains .NET code and supporting files for working with the Static Analysis Results Interchange Format (SARIF). For more information about SARIF, see the [SARIF Home Page](http://sarifweb.azurewebsites.net). You can read the [SARIF specification](https://rawgit.com/sarif-standard/sarif-spec/master/Static%20Analysis%20Results%20Interchange%20Format%20(SARIF).html), or file [issues](https://github.com/sarif-standard/sarif-spec/issues) in the [SARIF GitHub repo](https://github.com/sarif-standard/sarif-spec).

## Getting started

To add the SARIF SDK to your project, install the Sarif.Sdk [NuGet package](https://www.nuget.org/packages/Sarif.Sdk). Sarif.Sdk depends on [Newtonsoft.Json](http://www.newtonsoft.com/json), which is installed automatically when you install Sarif.Sdk.

The types in the SARIF SDK are in the `Microsoft.CodeAnalysis.Sarif` namespace.

The SARIF SDK provides a set of classes which represent the elements of the SARIF format. We refer to this as the "SARIF object model". The root type that represents a SARIF log file is `SarifLog`. Other types in the SARIF object model are `Result`, `PhysicalLocation`, _etc._.

Note: The SARIF SDK's build process automatically generates the SARIF object model classes from the SARIF JSON schema, which you can find at [`src/Sarif/Schemata/sarif-schema.json`](https://github.com/microsoft/sarif-sdk/blob/main/src/Sarif/Schemata/sarif-2.1.0-rtm.6.json). Although these files do exist in the repo (under [`src/Sarif/Autogenerated`](https://github.com/Microsoft/sarif-sdk/tree/main/src/Sarif/Autogenerated)), you should never edit them by hand.

In addition to the object model, the SARIF SDK provides a set of helper classes to facilitate using Newtonsoft.Json to read and write SARIF log files.

## Building the SDK

If you want to build the SDK from source, rather than consuming the NuGet package,
proceed as follows:

1. Install .NET Core SDK 2.1 and 3.1 from https://dotnet.microsoft.com/download

2. Ensure that Visual Studio 2019 is installed on your machine.

    You can build in VS 2017 as well.

3. Ensure that your Visual Studio installation includes the components that support
    - C# development

4. Open a Visual Studio 2019 Developer Command Prompt Window.

5. From the root directory of your local repo, run the command `BuildAndTest.cmd`.
    This restores all necessary NuGet packages, builds the SDK, and runs all the tests.

    All build output appears in the `bld\` subdirectory of the repo root directory.

    NOTE: You must run `BuildAndTest.cmd` once _before_ attempting to build in
    Visual Studio, to ensure that all required NuGet packages are available.

6. After you have run `BuildAndTest.cmd` once, you can open any of the solution files
in the `src\` directory in Visual Studio 2017, and build them by running **Rebuild Solution**.


## Accomplishing common tasks

To learn how to accomplish common tasks with the SARIF SDK, such as reading and writing files from disk,
see the [How To](https://github.com/Microsoft/sarif-sdk/blob/main/docs/how-to.md) page.

## Code of conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/),
or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
 