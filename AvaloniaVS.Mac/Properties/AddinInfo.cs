using Mono.Addins;

[assembly: Addin("AvaloniaVS.Mac",
	Namespace = "MonoDevelop",
	Version = "0.1",
	Category = "IDE extensions")]

[assembly: AddinName("XAML Previewer")]
[assembly: AddinDescription("Avalonia XAML Previewer")]

[assembly: AddinDependency("::MonoDevelop.Core", "17.0")]
[assembly: AddinDependency("::MonoDevelop.Ide", "17.0")]
