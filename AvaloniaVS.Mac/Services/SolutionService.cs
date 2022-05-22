using AvaloniaVS.Mac.Models;
using AvaloniaVS.Mac.Utils;
using MonoDevelop.Ide;
using MonoDevelop.Projects;


namespace AvaloniaVS.Mac.Services;


internal class SolutionService
{
    private const string NetFullToolPathKey = "AvaloniaPreviewerNetFullToolPath";
    private const string NetCoreToolPathKey = "AvaloniaPreviewerNetCoreToolPath";

    // TODO I probably need to find a better way of doing this...
    public bool IsAvaloniaProject(Project project)
    {
        var cfg = project.GetConfiguration(IdeApp.Workspace.ActiveConfiguration);
        var configuration = cfg as DotNetProjectConfiguration;

        return configuration.Properties.HasProperty(NetFullToolPathKey) || configuration.Properties.HasProperty(NetCoreToolPathKey);
    }

    public IReadOnlyList<ProjectInfo> GetProjects()
    {
        var result = new Dictionary<Project, ProjectInfo>();

        foreach (var prj in IdeApp.Workspace.GetAllProjects())
        {
            if (prj is DotNetProject proj)
            {
                if (!IsAvaloniaProject(proj))
                    continue;

                var cfg = proj.GetConfiguration(IdeApp.Workspace.ActiveConfiguration);
                var configuration = cfg as DotNetProjectConfiguration;

                var projectInfo = new ProjectInfo();
                projectInfo.Name = proj.Name;
                projectInfo.Project = proj;


                if (proj.CompileTarget == CompileTarget.Exe || proj.CompileTarget == CompileTarget.WinExe)
                {
                    projectInfo.IsExecutable = true;

                    //TODO Stop this hack
                    projectInfo.IsStartupProject = true;
                }
               
                List<Project> projectReferences = new List<Project>();
                List<string> assemblyReferences = new List<string>();

                foreach (var reference in proj.References)
                {
                    if (reference.IsProjectReference())
                    {
                        Console.WriteLine($"ProjectReference: {reference.ItemName}");
                        projectReferences.Add(reference.Project);
                    }

                    if (reference.IsAssemblyReference())
                    {
                        Console.WriteLine($"AssemblyReference: {reference.ItemName}");
                        assemblyReferences.Add(reference.GetFullAssemblyPath());
                    }
                }

                var previewerNetFullToolPath = configuration.Properties.GetProperty(NetFullToolPathKey);
                var previewerNetCoreToolPath = configuration.Properties.GetProperty(NetCoreToolPathKey);

                var hostAppPath = ResolvePreviewerPath(previewerNetCoreToolPath.Value);
                if (!File.Exists(@hostAppPath))
                {
                    throw new Exception($"Unable to find {hostAppPath}");
                }

                var mainOutput = new ProjectOutputInfo(configuration.CompiledOutputName, configuration.TargetFrameworkShortName, configuration.TargetFramework.Name, hostAppPath);
                projectInfo.Outputs = new List<ProjectOutputInfo> { mainOutput };

                if (Directory.Exists(configuration.OutputDirectory))
                {
                    Console.WriteLine($"OutputDirectory: {configuration.OutputDirectory}");

                }

                if (Directory.Exists(configuration.IntermediateOutputDirectory))
                {
                    Console.WriteLine($"IntermediateOutputDirectory: {configuration.IntermediateOutputDirectory}");
                }

                result.Add(proj, projectInfo);
            }
        }

        return result.Values.ToList();
    }

    // eh? what do here?
    private string ResolvePreviewerPath(string input)
    {
        var split = input.Split("\\");
        return Path.GetFullPath(Path.Combine(split), "/");
    }
}

