namespace SourceGenerator.Records;

public record ClassInfo(
           string Namespace,
           string ClassName,
           List<PropertyInfo> Properties,
           string Modifiers)
{
    public string NamespaceString => string.IsNullOrWhiteSpace(Namespace) ? 
        string.Empty : $"namespace {Namespace};";

}
