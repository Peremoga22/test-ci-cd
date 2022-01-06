///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS 
///////////////////////////////////////////////////////////////////////////////

var configurations = new List<string>() {
    "Debug",
    "Release"
};
var localBuildNumber = 111.ToString();
var target = Argument("target", "Default");
var configuration = Argument("configuration", configurations[1]);
var buildNumber = Argument("buildNumber", localBuildNumber); 
var commmitTag = Argument("commmitTag", "test-local-" + localBuildNumber).Split('-')[0].ToLower(); 
var framework = Argument("framework", "net5.0");
var runtime = Argument("runtime", "linux-x64");
///////////////////////////////////////////////////////////////////////////////
// PARAMS
///////////////////////////////////////////////////////////////////////////////

var name = "BlazorApp1";
var solution = $"{name}.sln";
var pathToArtifacts = MakeAbsolute(Directory("../")).Combine("Artifacts").Combine(name);
var pathToArtifact = string.Format("{0}/{1}", pathToArtifacts, buildNumber);
var leftNumberOfBuilds = 3;
var projectsToPublish = new List<string>() {
    "web-app"
};
///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
   // Executed BEFORE the first task.
   Information("Running tasks...");
});

Teardown(ctx =>
{
   // Executed AFTER the last task.
   Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////


Task("CleanArtifact")
.Does(() =>
{
   CleanDirectory(pathToArtifact);
});

Task("CleanArtifacts")
.IsDependentOn("CleanArtifact")
.Does(() =>
{
   if(GetSubDirectories(pathToArtifacts).Count() > leftNumberOfBuilds)
   {
      DeleteDirectories(
         GetSubDirectories(pathToArtifacts).OrderBy(z=>z.FullPath).Skip(leftNumberOfBuilds).ToList(),
         new DeleteDirectorySettings {
               Recursive = true,
               Force = true
            });
   }
});

Task("DotNetCoreClean")
.IsDependentOn("CleanArtifacts")
.Does(() => {
   DotNetCoreClean("./");
});
Task("DotNetCoreRestore")
.IsDependentOn("DotNetCoreClean")
.Does(() => {
   DotNetCoreRestore("./");
});
Task("DotNetCoreBuild")
.IsDependentOn("DotNetCoreRestore")
.Does(() => {
   DotNetCoreBuild("./");
});

Task("DotNetCorePublish")
.IsDependentOn("DotNetCoreBuild")
.Does(() => {
      foreach(var file in projectsToPublish) 
      {
         var proj = GetFiles($"./**/{file}.csproj").First();
         var tempPathToPublish = MakeAbsolute(Directory(pathToArtifact)).Combine(file).FullPath;

         var settings = new DotNetCorePublishSettings
         {
            Framework = framework,
            Runtime = runtime,
            SelfContained = true,  
            Configuration = configuration,
            OutputDirectory = tempPathToPublish
         };

         DotNetCorePublish(proj.FullPath, settings);
      }
});

Task("Default")
.IsDependentOn("DotNetCorePublish")
.Does(() => {
   Information("Hello Cake!");
});

RunTarget(target);