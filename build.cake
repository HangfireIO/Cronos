#addin nuget:?package=Cake.VersionReader
#tool "nuget:?package=xunit.runner.console"

Task("Restore-NuGet-Packages")
    .Does(()=> 
{
    NuGetRestore("Cronos.sln");
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(()=> 
{
    MSBuild("Cronos.sln", new MSBuildSettings 
    {
        ToolVersion = MSBuildToolVersion.VS2017,
        Configuration = "Release"
    }
    .WithProperty("TargetFramework", "net452"));
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    var testAssemblies = GetFiles("./tests/**/bin/Release/**/*.Tests.dll");
    XUnit2(testAssemblies);
});

Task("Pack")
    .IsDependentOn("Test")
    .Does(()=> 
{
    var version = GetVersionNumber("src/Cronos/bin/Release/netstandard1.0/Cronos.dll");
    
    var appveyorRepoTag = EnvironmentVariable("APPVEYOR_REPO_TAG");
    var appveyorBuildNumber = EnvironmentVariable("APPVEYOR_BUILD_NUMBER");

    if (appveyorRepoTag != "True" && appveyorBuildNumber != null) 
    {
        version += "-build-" + appveyorBuildNumber;
    }

    var appveyorRepoTagName = EnvironmentVariable("APPVEYOR_REPO_TAG_NAME");
    if(appveyorRepoTagName != null && appveyorRepoTagName.StartsWith("v"+version+"-"))
    {
        version = appveyorRepoTagName.Substring(1);
    }

    CreateDirectory("build");
    
    CopyFiles(GetFiles("./src/Cronos/bin/**/*.nupkg"), "build");
    Zip("./src/Cronos/bin/Release/netstandard1.0", "build/Cronos-" + version +".zip");
});

RunTarget("Pack");