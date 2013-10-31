## Building

To build NServiceBus just open `NServiceBus.sln` in Visual Studio.

Note that the debug build doesn't ilmerge and if you plan to use the binaries in test/production
you need to do a release build.

You'll find the built assemblies in /binaries.

If you see the build failing, check that you haven't put the source of NServiceBus in a deep subdirectory since long path names (greater than 248 characters) aren't supported by MSBuild.

## Running

To run NServiceBus, please download and install the setup file from http://www.nservicebus.com/Downloads.aspx

## Licenses

### [NHibernate](http://www.hibernate.org/)

NHibernate is licensed under the LGPL v2.1 license as described here:

http://www.hibernate.org/license.html

NHibernate binaries are merged into NServiceBus allowed under the LGPL license terms found here:

http://www.gnu.org/licenses/old-licenses/lgpl-2.1.txt


### Iesi.Collections 

Iesi.Collections binaries are merged into NServiceBus allowed under the license terms found here:

Copyright 2002-2004 by Aidant Systems, Inc., and by Jason Smith.

Copied from http://www.codeproject.com/csharp/sets.asp#xx703510xx that was posted by JasonSmith 12:13 2 Jan '04

Feel free to use this code any way you want to. As a favor to me, you can leave the copyright in there. You never know when someone might recognize your name! 

If you do use the code in a commercial product, I would appreciate hearing about it. This message serves as legal notice that I won't be suing you for royalties!  The code is in the public domain.

On the other hand, I don't provide support. The code is actually simple enough that it shouldn't need it. 

### [Fluent NHibernate](http://github.com/jagregory/fluent-nhibernate) 

Fluent NHibernate is licensed under the BSD license as described [here](http://github.com/jagregory/fluent-nhibernate/raw/master/LICENSE.txt).

Fluent NHibernate binaries are merged into NServiceBus allowed under the terms of the license.

### [Autofac](http://code.google.com/p/autofac/) 

Autofac is licensed under the MIT license as described [here](http://code.google.com/p/autofac/).

Autofac binaries are linked into the NServiceBus distribution allowed under the license terms found [here](http://www.opensource.org/licenses/mit-license.php).

### [Spring.NET](http://www.springframework.net)

Spring.NET is licensed under the Apache license version 2.0 as described [here](http://www.springframework.net/license.html)

Spring.NET binaries are merged into NServiceBus allowed under the license terms found [here](http://www.apache.org/licenses/LICENSE-2.0.txt).

### [Antlr](http://antlr.org)

Antlr is licensed under the BSD license as described [here](http://antlr.org/license.html).

Antlr binaries are merged into NServiceBus allowed under the license terms described above.

### [Common.Logging](http://netcommon.sourceforge.net)

Common.Logging is licensed under the Apache License, Version 2.0 as described [here](http://netcommon.sourceforge.net/license.html).

Common.Logging binaries are merged into NServiceBus allowed under the LGPL license terms found [here](http://www.apache.org/licenses/LICENSE-2.0.txt).

### [StructureMap](http://structuremap.net)

StructureMap is licensed under the Apache License, Version 2.0 as described [here](http://docs.structuremap.net/).

StructureMap binaries are linked into the NServiceBus distribution allowed under the license terms found [here](http://www.apache.org/licenses/LICENSE-2.0.txt).

### [Castle](http://www.castleproject.org/)

Castle is licensed under the Apache License, Version 2.0 as described [here](http://www.castleproject.org/).

Castle binaries are linked into the NServiceBus distribution allowed under the license terms found [here](http://www.apache.org/licenses/LICENSE-2.0.txt).

### [Unity](http://unity.codeplex.com)

Unity is licensed under the MSPL license as described [here](http://unity.codeplex.com/license).

Unity binaries are linked into the NServiceBus distribution allowed under the license terms described above.

### [Log4Net](http://logging.apache.org/log4net/)

Log4Net is licensed under the Apache License, Version 2.0 as described [here](http://logging.apache.org/log4net/license.html).

Log4Net binaries are linked into the NServiceBus distribution allowed under the license terms described above.

### [TopShelf](http://topshelf-project.com/)

TopShelf is licensed under the Apache License, Version 2.0 as described here:

TopShelf binaries are merged into NServiceBus as allowed under the license terms described [here](http://www.apache.org/licenses/LICENSE-2.0.txt).

### [Rhino Mocks](http://www.hibernatingrhinos.com/oss/rhino-mocks)

Rhino Mocks is licensed under the BSD License as described [here](http://www.hibernatingrhinos.com/oss/rhino-mocks).

Rhino Mocks binaries are merged into NServiceBus allowed under the license terms described [here](http://www.opensource.org/licenses/bsd-license.php).

### [RavenDB](http://ravendb.net)

RavenDB is under both a OSS and a commercial license described [here](http://ravendb.net/licensing).

The commercial version can be used free of charge for NServiceBus specific storage needs like:

Subscriptions, Sagas, Timeouts, etc 

Application specific use requires a paid RavenDB license

RavenDB binaries are linked into the NServiceBus distribution allowed under the license terms described above.

### [ActiveMQ](http://activemq.apache.org)

ActiveMQ is licensed under the Apache 2.0 licence  as described [here](http://activemq.apache.org/what-is-the-license.html).

The ActiveMQ client is referenced by NServiceBus



