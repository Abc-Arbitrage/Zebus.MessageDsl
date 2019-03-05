
# Build-Time Code Generator

## How to use

 - Install the [`Zebus.MessageDsl.Build`](https://www.nuget.org/packages/Zebus.MessageDsl.Build) NuGet package into your project
 - Add a file with the `.msg` extension and write your message definitions there
 - If needed, you can customize the namespace by changing the "Custom Tool Namespace" property of the file
 - If you use the "Go to Definition" feature of the IDE on a message class, you will see the generated C# file. Do not edit it manually.

## How it works

Visual Studio will call a MSBuild target which will generate C# code every time a `.msg` file changes (so IntelliSense can work correctly), and also when the project is built. These files are stored in the `obj\ZebusMessages` directory. Incremental builds are supported, which means only out-of-date files are updated, but rebuilding the project will force an update.

In more detail, this package declares a [custom project item type](https://github.com/Microsoft/VSProjectSystem/blob/master/doc/extensibility/custom_item_types.md), and leverages [design-time builds](https://github.com/dotnet/project-system/blob/master/docs/design-time-builds.md) to trigger code generation.
