using System;

[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class RuntimeFunctionAttribute : Attribute
{
    public string Description { get; }

    public RuntimeFunctionAttribute(string description)
    {
        Description = description;
    }

    public RuntimeFunctionAttribute()
    {
        Description = "Use arrays and don't reinitialize variables.";
    }
}