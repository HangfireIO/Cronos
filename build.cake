#addin nuget:?package=Cake.VersionReader
#tool "nuget:?package=xunit.runner.console"

var version = "0.2.0";
var configuration = "Release";

Task("Restore-NuGet-Packages")
    .Does(()=> 
{
    DotNetCoreRestore();
});

Task("Clean")
    .Does(()=> 
{
    CleanDirectory("./build");
    StartProcess("dotnet", "clean -c:" + configuration);
});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(()=> 
{
    DotNetCoreBuild("src/Cronos/Cronos.csproj",  new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        ArgumentCustomization = args => args.Append("/p:Version=" + version)
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetCoreTest("./tests/Cronos.Tests/Cronos.Tests.csproj", new DotNetCoreTestSettings
    {
        Configuration = "Release",
        ArgumentCustomization = args => args.Append("/p:BuildProjectReferences=false")
    });
});

Task("AppVeyor")
    .IsDependentOn("Test")
    .Does(()=> 
{
    if (AppVeyor.Environment.Repository.Tag.IsTag) 
    {
        var tagName = AppVeyor.Environment.Repository.Tag.Name;
        if(tagName.StartsWith("v"))
        {
            version = tagName.Substring(1);
        }
    }
    else
    {
        version += "-build-0" + AppVeyor.Environment.Build.Number;
    }

    AppVeyor.UpdateBuildVersion(version);
});

Task("Local")
    .Does(()=> 
{
    RunTarget("Test");
});

Task("Pack")
    .Does(()=> 
{
    var target = AppVeyor.IsRunningOnAppVeyor ? "AppVeyor" : "Local";
    RunTarget(target);

    CreateDirectory("build");
    
    Zip("./src/Cronos/bin/" + configuration, "build/Cronos-" + version +".zip");
});
    
RunTarget("Pack");