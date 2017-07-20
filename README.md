## Building

To build NServiceBus just open `NServiceBus.sln` in Visual Studio.

You'll find the built assemblies in /binaries.

If you see the build failing, check that you haven't put the source of NServiceBus in a deep subdirectory since long path names (greater than 248 characters) aren't supported by MSBuild.


## Licensing

### NServiceBus

NServiceBus is licensed under the RPL 1.5 license. More details can be found [here](LICENSE.md).

### [LightInject](http://www.lightinject.net/) 

LightInject is licensed under the MIT license as described [here](http://www.lightinject.net/licence/).

LightInject sources are compiled into the NServiceBus distribution as allowed under the license terms found [here](http://www.lightinject.net/licence/).

### [SimpleJson](https://github.com/facebook-csharp-sdk/simple-json/) 

SimpleJson is licensed under the MIT license as described [here](https://github.com/facebook-csharp-sdk/simple-json/blob/master/LICENSE.txt).

SimpleJson sources are compiled into the NServiceBus distribution as allowed under the license terms found [here](https://github.com/facebook-csharp-sdk/simple-json/blob/master/LICENSE.txt).

### [Json.NET](http://www.newtonsoft.com/json)

Json.NET is licensed under the MIT license as described [here](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

Json.NET binaries are linked into the NServiceBus distribution as allowed under the license terms found [here](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
