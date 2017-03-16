#tool "nuget:?package=xunit.runner.console"
#addin "Cake.FileHelpers"

// Don't edit manually! Use `.\build.ps1 -ScriptArgs '--newVersion="*.*.*"'` command instead!
var version = "0.2.0";

var configuration = Argument("configuration", "Release");
var newVersion = Argument("newVersion", version);
var target = Argument("target", "Pack");

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

Task("Version")
    .Does(() => 
    {
        if(newVersion == version) return;

        var versionRegex = @"[0-9]+(\.([0-9]+|\*)){1,3}";
        var cakeRegex = "var version = \"" + versionRegex + "\"";
        
        ReplaceRegexInFiles("build.cake", cakeRegex, "var version = \"" + newVersion + "\"");
        ReplaceRegexInFiles("appveyor.yml", "version: " + versionRegex, "version: " + newVersion + "");
    });

Task("Build")
    .IsDependentOn("Version")
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
    
RunTarget(target);