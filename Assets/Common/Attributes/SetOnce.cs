using System;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
sealed class SetOnce : Attribute
{
}
