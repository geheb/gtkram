namespace GtKram.WebApp.Routing;

public class MenuItem
{
    public string Name { get; set; }
    public string Path { get; set; }

    public MenuItem(string name, string path)
    {
        Name = name;
        Path = path;
    }
}