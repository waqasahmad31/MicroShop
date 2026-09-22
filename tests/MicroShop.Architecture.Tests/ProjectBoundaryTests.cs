using System.Xml.Linq;
using Xunit;

namespace MicroShop.Architecture.Tests;

public sealed class ProjectBoundaryTests
{
    private static readonly string Root = FindRepositoryRoot();
    private static readonly string[] Services = ["Identity", "Catalog", "Inventory", "Ordering"];

    private static readonly Dictionary<string, string[]> ExpectedReferences = CreateExpectedReferences();

    [Fact]
    public void Solution_contains_every_expected_project()
    {
        var projects = ReadProjects();
        Assert.Equal(ExpectedReferences.Keys.Order(), projects.Keys.Order());

        var solution = File.ReadAllText(Path.Combine(Root, "MicroShop.sln"));
        foreach (var project in projects.Values)
        {
            var relative = Path.GetRelativePath(Root, project.Path).Replace('/', '\\');
            Assert.Contains($"\"{relative}\"", solution);
        }
    }

    [Fact]
    public void Project_references_follow_the_documented_dependency_direction()
    {
        foreach (var (name, project) in ReadProjects())
        {
            var references = project.Xml.Descendants("ProjectReference")
                .Select(element => ResolveReference(project.Path, element))
                .ToArray();

            Assert.All(references, reference => Assert.True(File.Exists(reference), reference));
            Assert.Equal(ExpectedReferences[name].Order(),
                references.Select(path => Path.GetFileNameWithoutExtension(path)).Order());
        }
    }

    [Fact]
    public void Project_reference_graph_has_no_cycles()
    {
        var projects = ReadProjects();
        var visited = new HashSet<string>();
        var active = new HashSet<string>();

        foreach (var project in projects.Keys)
        {
            Visit(project);
        }

        void Visit(string name)
        {
            Assert.DoesNotContain(name, active);
            if (!visited.Add(name))
            {
                return;
            }

            active.Add(name);
            foreach (var reference in projects[name].Xml.Descendants("ProjectReference"))
            {
                Visit(Path.GetFileNameWithoutExtension(ResolveReference(projects[name].Path, reference)));
            }
            active.Remove(name);
        }
    }

    [Fact]
    public void Domain_projects_are_framework_and_infrastructure_independent()
    {
        var projects = ReadProjects();
        var sharedProperties = XDocument.Load(Path.Combine(Root, "Directory.Build.props"));

        Assert.Empty(sharedProperties.Descendants("PackageReference"));
        Assert.Empty(sharedProperties.Descendants("FrameworkReference"));
        Assert.Empty(sharedProperties.Descendants("ProjectReference"));

        foreach (var service in Services)
        {
            var domain = projects[$"{service}.Domain"].Xml;
            Assert.Equal("Microsoft.NET.Sdk", domain.Root!.Attribute("Sdk")!.Value);
            Assert.Empty(domain.Descendants("PackageReference"));
            Assert.Empty(domain.Descendants("FrameworkReference"));
            Assert.Empty(domain.Descendants("ProjectReference"));
        }
    }

    private static Dictionary<string, string[]> CreateExpectedReferences()
    {
        var references = new Dictionary<string, string[]>
        {
            ["MicroShop.Gateway"] = [],
            ["MicroShop.Web"] = ["MicroShop.Web.Client"],
            ["MicroShop.Web.Client"] = [],
            ["Notification.Service"] = [],
            ["MicroShop.Contracts"] = [],
            ["MicroShop.Messaging"] = [],
            ["MicroShop.Observability"] = [],
            ["MicroShop.Architecture.Tests"] = []
        };

        foreach (var service in Services)
        {
            references[$"{service}.Api"] = [$"{service}.Application", $"{service}.Infrastructure"];
            references[$"{service}.Application"] = [$"{service}.Domain"];
            references[$"{service}.Domain"] = [];
            references[$"{service}.Infrastructure"] = [$"{service}.Application", $"{service}.Domain"];
        }

        return references;
    }

    private static Dictionary<string, (string Path, XDocument Xml)> ReadProjects() =>
        new[] { "src", "tests" }
            .SelectMany(directory => Directory.EnumerateFiles(
                Path.Combine(Root, directory), "*.csproj", SearchOption.AllDirectories))
            .Where(path => !Path.GetRelativePath(Root, path)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => segment is "obj" or "bin"))
            .ToDictionary(path => Path.GetFileNameWithoutExtension(path),
                path => (path, XDocument.Load(path)));

    private static string ResolveReference(string projectPath, XElement reference) =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!,
            reference.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MicroShop.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Run architecture tests from a checkout containing MicroShop.sln.");
    }
}

