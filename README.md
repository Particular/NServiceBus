## Building

To build NServiceBus just open `NServiceBus.sln` in Visual Studio.

Note that the debug build doesn't ilmerge and if you plan to use the binaries in test/production
you need to do a release build.

You'll find the built assemblies in /binaries.

If you see the build failing, check that you haven't put the source of NServiceBus in a deep subdirectory since long path names (greater than 248 characters) aren't supported by MSBuild.


## Licensing

### NServiceBus

NServiceBus is licensed under the RPL 1.5 license. More details can be found [here](LICENSE.md).

### [Autofac](http://code.google.com/p/autofac/) 

Autofac is licensed under the MIT license as described [here](https://github.com/autofac/Autofac/blob/master/LICENSE).

Autofac binaries are linked into the NServiceBus distribution allowed under the license terms found [here](https://github.com/autofac/Autofac/blob/master/LICENSE).

### [Json.NET](http://www.newtonsoft.com/json)

Json.NET is licensed under the MIT license as described [here](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

Json.NET binaries are linked into the NServiceBus distribution allowed under the license terms found [here](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
