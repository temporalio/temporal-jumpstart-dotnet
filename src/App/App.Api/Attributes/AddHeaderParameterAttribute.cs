namespace App.Api.Attributes;

// AddHeaderParameter allows you to annotate controller methods with headers
// that should impact middleware, but not bind to an attribute (like with FromHeaderAttribute).
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class AddHeaderParameterAttribute : Attribute
{
    public required string Name { get; set; }
    public bool Required { get; set; }
}