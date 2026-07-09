using System;
namespace SNCoreEditor;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
sealed class UninitializedContextAttribute : Attribute { }
