namespace Qala.Framework.Application.Routing;

/// <summary>Standard endpoint routes. Reads are POST with a body so filters never land in URLs or logs.</summary>
public static class RouteDefinitions
{
    public const string List = "list";
    public const string Lookup = "lookup";
    public const string GetById = "getbyid";
    public const string Create = "";
    public const string Update = "";
    public const string Delete = "";
    public const string Activate = "activate";
    public const string Deactivate = "deactivate";
    public const string Export = "export";
    public const string Import = "import";
    public const string Me = "me";
}
