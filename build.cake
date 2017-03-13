#addin nuget:?package=Cake.VersionReader
#tool "nuget:?package=xunit.runner.console"

var version = "0.1.1";

Task("Restore-NuGet-Packages")
    .Does(()=> 
{
    DotNetCoreRestore();
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(()=> 
{
    DotNetCoreBuild("src/Cronos/Cronos.csproj",  new DotNetCoreBuildSettings
    {
        Configuration = "Release",
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

    CreateDirectory("build");
    
    CopyFiles(GetFiles("./src/Cronos/bin/**/*.nupkg"), "build");
    Zip("./src/Cronos/bin/Release/netstandard1.0", "build/Cronos-" + version +".zip");
});

Task("Local")
    .IsDependentOn("Test")
    .Does(()=> 
{
    CreateDirectory("build");
    
    CopyFiles(GetFiles("./src/Cronos/bin/**/*.nupkg"), "build");
    Zip("./src/Cronos/bin/Release/netstandard1.0", "build/Cronos-" + version +".zip");
});

Task("Pack")
    .Does(()=> 
{
    if (AppVeyor.IsRunningOnAppVeyor)
    {
        RunTarget("AppVeyor");
    }
    else
    {
        RunTarget("Local");
    }
});
    
RunTarget("Pack");