using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;

using MonoDevelop.Components.Declarative;
using AvaloniaVS.Mac.Services;
using AppKit;
using WebKit;

namespace AvaloniaVS.Mac.Views;

/*
public class PreviewerPadContent : PadContent
{
	//UI 
	PreviewerWidget previewerWidget;
	Control control;
	ToolbarButtonItem startButton;
	ToolbarButtonItem stopButton;

	
	bool enabled;
	PreviewerProcess previewerProcess;
	Document document;
	SolutionService solutionService;
	IReadOnlyList<ProjectInfo> projectsInfo;


	public static PreviewerPadContent Instance { get; private set; }

	public PreviewerPadContent()
	{
		Instance = this;
		previewerWidget = new PreviewerWidget();
	}

	protected override void Initialize(IPadWindow window)
	{
		previewerProcess = new PreviewerProcess();
		solutionService = new SolutionService();
		projectsInfo = solutionService.GetProjects();

		CreateToolbar(window);
	}

	public override Control Control
	{
		get
		{
			if (control == null)
				control = new XwtControl(previewerWidget);
			
			// Returning control does not work.
			return control.GetNativeWidget<AppKit.NSView>();
		}
	}

	public override void Dispose()
	{
		if (previewerWidget != null)
		{
			previewerWidget.Dispose();
			previewerWidget = null;
		}
		base.Dispose();
	}

	public async Task SetDocument(Document doc)
    {
		if (doc == null)
			return;

		if (doc.FilePath.Extension != ".axaml")
			return;

		doc.TextBuffer.Changed -= HandleDocumentTextChanged;

		document = doc;
        document.TextBuffer.Changed += HandleDocumentTextChanged;

		var selectedProject = projectsInfo.FirstOrDefault(x => x.Project == IdeApp.Workspace.CurrentSelectedProject);
		if (selectedProject == null)
			return;

		var selectedProjOutput = selectedProject.Outputs.FirstOrDefault();
		if (selectedProjOutput == null)
			return;

		var assemblyPath = @"/Users/michaeljames/RiderProjects/RadioButton/RadioButton/bin/Debug/net6.0/RadioButton.dll";
		var executablePath = selectedProjOutput.TargetAssembly.Replace(".exe", ".dll"); 
		var hostAppPath = selectedProjOutput.HostApp;

		if (!previewerProcess.IsRunning)
			await previewerProcess.StartAsync(assemblyPath, executablePath, hostAppPath);

		var content = document.TextBuffer.CurrentSnapshot.GetText();
		if(!string.IsNullOrEmpty(content))
			await previewerProcess.UpdateXamlAsync(content);

	}

	public void ReloadProjects()
    {
		projectsInfo = solutionService.GetProjects();
    }

	public void Stop()
    {
		if(previewerProcess.IsRunning)
        {
			previewerProcess.Stop();
        }
    }

	public bool IsRunning => previewerProcess.IsRunning;

	public bool IsReady => previewerProcess.IsReady;

	public Document ActiveDocument => document;

    private async void HandleDocumentTextChanged(object? sender, Microsoft.VisualStudio.Text.TextContentChangedEventArgs e)
    {
		var content = document.TextBuffer.CurrentSnapshot.GetText();
		if (!string.IsNullOrEmpty(content))
			await previewerProcess.UpdateXamlAsync(content);
	}

	private void CreateToolbar(IPadWindow window)
    {
		var toolbar = new Toolbar();

		startButton = new ToolbarButtonItem(toolbar.Properties, nameof(startButton));
		startButton.Icon = Stock.RunProgramIcon;
		startButton.Clicked += StartButtonClicked;
		startButton.Tooltip = GettextCatalog.GetString("Start the previewer");
		toolbar.AddItem(startButton);

		stopButton = new ToolbarButtonItem(toolbar.Properties, nameof(stopButton));
		stopButton.Icon = Stock.Stop;
		stopButton.Clicked += StopButtonClicked;
		stopButton.Tooltip = GettextCatalog.GetString("Stop the previewer");
		// Cannot disable the button before the underlying NSView is created.
		//stopButton.Enabled = false;
		toolbar.AddItem(stopButton);


		window.SetToolbar(toolbar, DockPositionType.Top);

		stopButton.Enabled = false;
	}

	private void StartButtonClicked(object sender, EventArgs e)
	{
		enabled = true;
		startButton.Enabled = false;
		stopButton.Enabled = true;

		OnEnabledChanged();
	}

	private void StopButtonClicked(object sender, EventArgs e)
	{
		enabled = false;
		startButton.Enabled = true;
		stopButton.Enabled = false;

		OnEnabledChanged();
	}

	private async void OnEnabledChanged()
	{
		if (enabled)
		{
			if (!previewerProcess.IsRunning && document != null)
			{
				Console.WriteLine("Enabling Previewer");
				await SetDocument(document);
			}
			else
            {
				Console.WriteLine("Unable to enable Previewer");
			}
			previewerWidget.Refresh();
		}
		else
		{
			Console.WriteLine("Disabled Previewer");

			if (previewerProcess.IsRunning)
				previewerProcess.Stop();
		}
	}
}

*/


public class XamlPreviewerPad : PadContent
{
	PreviewerProcess previewerProcess;
	PreviewerWidget widget;
	ToolbarButtonItem startButton;
	ToolbarButtonItem stopButton;


	bool enabled;

	public static XamlPreviewerPad Instance { get; private set; }

	public XamlPreviewerPad(PreviewerProcess previewerProcess)
    {
		widget = new PreviewerWidget();

		Instance = this;

		this.previewerProcess = previewerProcess;

        previewerProcess.FrameReceived += PreviewerProcess_FrameReceived;
        previewerProcess.ErrorChanged += PreviewerProcess_ErrorChanged;
        previewerProcess.ProcessExited += PreviewerProcess_ProcessExited;
    }

    private void PreviewerProcess_ProcessExited(object? sender, EventArgs e)
    {
		Console.WriteLine("Previewer Process Exited");
    }

    private void PreviewerProcess_ErrorChanged(object? sender, EventArgs e)
    {
		Console.WriteLine("Previewer Error");
	}

    private void PreviewerProcess_FrameReceived(object? sender, EventArgs e)
    {
		widget.SetImage(previewerProcess.PreviewImage);

		Console.WriteLine("Previewer Frame Recieved");
		
	}

	/*
    protected override void Initialize(IPadWindow window)
	{
		var toolbar = new Toolbar();

		startButton = new ToolbarButtonItem(toolbar.Properties, nameof(startButton));
		startButton.Icon = Stock.RunProgramIcon;
		startButton.Clicked += StartButtonClicked;
		startButton.Tooltip = GettextCatalog.GetString("Start the previewer");
		toolbar.AddItem(startButton);

		stopButton = new ToolbarButtonItem(toolbar.Properties, nameof(stopButton));
		stopButton.Icon = Stock.Stop;
		stopButton.Clicked += StopButtonClicked;
		stopButton.Tooltip = GettextCatalog.GetString("Stop the previewer");
		// Cannot disable the button before the underlying NSView is created.
		//stopButton.Enabled = false;
		toolbar.AddItem(stopButton);

		
		window.SetToolbar(toolbar, DockPositionType.Top);

		stopButton.Enabled = false;
		
	}
	*/

	Control control;

	public override Control Control
	{
		get
		{
			if(control == null)
            {
				control = new XwtControl(widget);
            }
			return control.GetNativeWidget<NSView>();
		}
	}
	

	void StartButtonClicked(object sender, EventArgs e)
	{
		enabled = true;
		startButton.Enabled = false;
		stopButton.Enabled = true;

		OnEnabledChanged();
	}

	void StopButtonClicked(object sender, EventArgs e)
	{
		enabled = false;
		startButton.Enabled = true;
		stopButton.Enabled = false;

		OnEnabledChanged();
	}

	void OnEnabledChanged()
	{
		if (enabled)
		{
			Console.WriteLine("Enabled");
		}
		else
		{
			Console.WriteLine("Disabled");
		}
	}
}