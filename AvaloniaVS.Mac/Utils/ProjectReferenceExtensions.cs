using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace AvaloniaVS.Mac.Utils;

internal static class ProjectReferenceExtensions
{
	public static bool IsProjectReference(this ProjectReference reference)
	{
		return reference.ReferenceType == ReferenceType.Project;
	}

	public static bool IsAssemblyReference(this ProjectReference reference)
	{
		return (reference.ReferenceType == ReferenceType.Assembly)
			|| ((reference.ReferenceType == ReferenceType.Package) && !reference.IsValid);
	}

	public static bool IsReferenceFromPackage(this ProjectReference projectReference, FilePath packagesFolderPath)
	{
		if (!projectReference.IsAssemblyReference())
			return false;

		var project = projectReference.OwnerProject as DotNetProject;
		if (project == null)
			return false;

		var assemblyFilePath = new FilePath(projectReference.GetFullAssemblyPath());
		if (assemblyFilePath.IsNullOrEmpty)
			return false;

		return assemblyFilePath.IsChildPathOf(packagesFolderPath);
	}

	public static string GetFullAssemblyPath(this ProjectReference projectReference)
	{
		if (!String.IsNullOrEmpty(projectReference.HintPath))
		{
			return Path.GetFullPath(projectReference.HintPath);
		}

		return null;
	}
}

