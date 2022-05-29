using System;
using System.Reflection;
using AvaloniaVS.Mac.Models;
using AvaloniaVS.Mac.Services;
using AvaloniaVS.Mac.Views;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace AvaloniaVS.Mac;

public class StartupHandler : CommandHandler
{
   // private PreviewerPadContent previewerPadContent;
    private XamlPreviewerPad previewerPadContent2;
    private Pad previewerPad;

    private IReadOnlyList<ProjectInfo> projectsInfo;
    private PreviewerProcess previewerProcess;

    private readonly string padName = "AvaloniaVS.Mac.PreviewerPad";
    private readonly string padTitle = "XAML Previewer";
    private readonly DockItemStatus padDockStatus = DockItemStatus.Dockable;
    private readonly string padIcon = Stock.XmlFileIcon;

    protected override void Run()
    {
        IdeApp.Initialized += delegate
        {
            IdeApp.Workspace.SolutionLoaded += OnSolutionLoaded;
            IdeApp.Workspace.SolutionUnloaded += OnSolutionUnloaded;


            IdeApp.Workspace.WorkspaceItemOpened += OnWorkspaceItemOpened;
            IdeApp.Workbench.ActiveDocumentChanged += OnActiveDocumentChanged;
        };
    }


    /*
    private async void OnActiveDocumentChanged(object? sender, DocumentEventArgs e)
    {
        if (e.Document == null)
            return;

        if (previewerPadContent == null)
            previewerPadContent = new PreviewerPadContent();

        if(previewerPad == null)
            previewerPad = IdeApp.Workbench.ShowPad(previewerPadContent, padName, padTitle, "Right", padDockStatus, padIcon);

        await previewerPadContent.SetDocument(e.Document);
        previewerPad.Visible = e.Document.FilePath.Extension == ".axaml";
    }
    */


    private async void OnActiveDocumentChanged(object? sender, MonoDevelop.Ide.Gui.DocumentEventArgs e)
    {
        var doc = e.Document;
        if (doc == null)
            return;

        Console.WriteLine($"OnActiveDocumentChanged: {doc.Name}");

        if (doc.FilePath.Extension == ".axaml")
        {
            previewerProcess = new PreviewerProcess();

            var pt = IdeApp.Workspace.BaseDirectory;
            Console.WriteLine(pt.FullPath);

            var statupProject = IdeApp.Workspace.CurrentSelectedSolution.StartupItem as Project;
            var selectedProject = IdeApp.Workspace.CurrentSelectedProject;

            var startupProjectInfo = projectsInfo.FirstOrDefault(x => x.Project == statupProject);
            if (startupProjectInfo == null)
                return;

            var startupProjectOutput = startupProjectInfo.Outputs.FirstOrDefault();
            if (startupProjectOutput == null)
                return;

            var selectedProjectInfo = projectsInfo.FirstOrDefault(x => x.Project == selectedProject);
            if (selectedProjectInfo == null)
                return;

            var selectedProjectOutput = selectedProjectInfo.Outputs.FirstOrDefault();
            if (selectedProjectOutput == null)
                return;


            var assemblyPath = selectedProjectOutput.TargetAssembly;
            var executablePath = startupProjectOutput.TargetAssembly.Replace(".exe", ".dll");
            var hostAppPath = startupProjectOutput.HostApp;

            if(!previewerProcess.IsRunning)
                await previewerProcess.StartAsync(assemblyPath, executablePath, hostAppPath);

            var content = doc.TextBuffer.CurrentSnapshot.GetText();
            previewerProcess.UpdateXamlAsync(content);

            doc.TextBuffer.Changed += (object? sender, Microsoft.VisualStudio.Text.TextContentChangedEventArgs e) =>
            {
                var content = doc.TextBuffer.CurrentSnapshot.GetText();
                previewerProcess.UpdateXamlAsync(content);
            };

            if (previewerPadContent2 == null)
            {
                previewerPadContent2 = new XamlPreviewerPad(previewerProcess);
                previewerPad = IdeApp.Workbench.ShowPad(previewerPadContent2, "AvaloniaVS.Mac.XamlPreviewerPad", "XAML Previewer", "Right", MonoDevelop.Components.Docking.DockItemStatus.Dockable, Stock.XmlFileIcon);

                var padContent = previewerPadContent2.Control;
                Console.WriteLine(padContent.GetType());
            }
            previewerPad.Visible = true;

        }
        else
        {
            if (previewerPad != null)
                previewerPad.Visible = false;
        }

    }


    private void OnWorkspaceItemOpened(object? sender, WorkspaceItemEventArgs e)
    {
        var workspaceItem = e.Item;
        if (workspaceItem == null)
            return;

        Console.WriteLine($"OnWorkspaceItemOpened: {workspaceItem.Name}");

    }

    void OnSolutionLoaded(object sender, SolutionEventArgs e)
    {
        projectsInfo = new SolutionService().GetProjects();
        /*
        if (previewerPadContent != null)
            previewerPadContent.ReloadProjects();
        */
    }

    void OnSolutionUnloaded(object sender, SolutionEventArgs e)
    {
        /*
        if (previewerPadContent != null)
        {
            previewerPadContent.Stop();
            previewerPadContent.ReloadProjects();
        }*/

        projectsInfo = new SolutionService().GetProjects();
    }
  
}

