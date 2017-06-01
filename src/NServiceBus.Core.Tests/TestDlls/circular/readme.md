## To recreate the assemblies

* Create ClassLibraryA and ClassLibraryB where B already references A
* Using the mono.cecil nuget do:

 ```
	var dll = "ClassLibraryA.dll";
    var temp = "temp.dll";
    
	File.Copy(dll, temp, true);
    
	using (var moduleDefinition = ModuleDefinition.ReadModule(temp))
    {
		moduleDefinition.AssemblyReferences.Add(new AssemblyNameReference("ClassLibraryB", null));
		moduleDefinition.Write(dll);
	}
```