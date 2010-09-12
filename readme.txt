NServiceBus is a non-trivial framework that takes time to understand.

The best way to get up and running is from the Samples. Run them, change them a bit, look at what references what. If you want to do your own thing, copy one of the samples (like FullDuplex), and change from there.


============
= Building =
============

In order to build the source, run the build.bat file.

You'll find the built assemblies in /build/output.

The satellite processes (distributor, timeout manager, and tools) will be in the adjacent directories.

If you see CS1668 warning when building under 2008, go to the 'C:\Program Files\Microsoft SDKs\Windows\v6.0A' directory and create the 'lib' subdirectory.

If you see the build failing, check that you haven't put nServiceBus in a deep subdirectory since long path names (greater than 248 characters) aren't supported by MSBuild.


===========
= Running =
===========

To run NServiceBus Msmq and MSDTC need to be installed and configured on your machine. To do this please
run RunMeFirst.bat.

Running an nServiceBus process is simple. Any external dependencies that are needed like MSMQ, MSDTC, databases will be set up automatically at startup if they aren't found.


=========
= Sagas =
=========

The 'Manufacturing' sample shows the use of sagas.


============
= Licenses =
============

NHibernate is licensed under the LGPL v2.1 license as described here:

http://www.hibernate.org/license.html

NHibernate binaries are merged into NServiceBus allowed under the LGPL license terms found here:

http://www.gnu.org/licenses/old-licenses/lgpl-2.1.txt

******************************


LinFu is licensed under the LGPL v3 license as described here:

http://code.google.com/p/linfu/

LinFu binaries are merged into NServiceBus allowed under the LGPL license terms found here:

http://www.gnu.org/licenses/lgpl-3.0.txt

******************************


Iesi.Collections binaries are merged into NServiceBus allowed under the license terms found here:

Copyright © 2002-2004 by Aidant Systems, Inc., and by Jason Smith.

Copied from http://www.codeproject.com/csharp/sets.asp#xx703510xx that was posted by JasonSmith 12:13 2 Jan '04

Feel free to use this code any way you want to. As a favor to me, you can leave the copyright in there. You never know when someone might recognize your name! 

If you do use the code in a commercial product, I would appreciate hearing about it. This message serves as legal notice that I won't be suing you for royalties!  The code is in the public domain.

On the other hand, I don't provide support. The code is actually simple enough that it shouldn't need it. 

******************************


Fluent NHibernate is licensed under the BSD license as described here:

http://github.com/jagregory/fluent-nhibernate/raw/master/LICENSE.txt

Fluent NHibernate binaries are merged into NServiceBus allowed under the terms of the license.

******************************


Autofac is licensed under the MIT license as described here:

http://code.google.com/p/autofac/

Autofac binaries are linked into the NServiceBus distribution allowed under the license terms found here:

http://www.opensource.org/licenses/mit-license.php

******************************

Spring.NET is licensed under the Apache license version 2.0 as described here:

http://www.springframework.net/license.html

Spring.NET binaries are merged into NServiceBus allowed under the license terms found here:

http://www.apache.org/licenses/LICENSE-2.0.txt

******************************


Antlr is licensed under the BSD license as described here:

http://antlr.org/license.html

Antlr binaries are merged into NServiceBus allowed under the license terms described above.

******************************


Common.Logging is licensed under the Apache License, Version 2.0 as described here:

http://netcommon.sourceforge.net/license.html

Common.Logging binaries are merged into NServiceBus allowed under the LGPL license terms found here:

http://www.apache.org/licenses/LICENSE-2.0.txt

******************************


StructureMap is licensed under the Apache License, Version 2.0 as described here:

http://structuremap.github.com/structuremap/index.html

StructureMap baries are linked into the NServiceBus distribution allowed under the license terms found here:

http://www.apache.org/licenses/LICENSE-2.0.txt

******************************


Castle is licensed under the Apache License, Version 2.0 as described here:

http://www.castleproject.org/

Castle binaries are linked into the NServiceBus distribution allowed under the license terms found here:

http://www.apache.org/licenses/LICENSE-2.0.txt

******************************


Unity is licensed under the MSPL license as described here:

http://unity.codeplex.com/license

Unity binaries are linked into the NServiceBus distribution allowed under the license terms described above.

******************************


Log4Net is licensed under the Apache License, Version 2.0 as described here:

http://logging.apache.org/log4net/license.html

Log4Net binaries are linked into the NServiceBus distribution allowed under the license terms described above.

******************************


TopShelf is licensed under the Apache License, Version 2.0 as described here:

http://code.google.com/p/topshelf/

TopShelf binaries are merged into NServiceBus as allowed under the license terms described here:

http://www.apache.org/licenses/LICENSE-2.0.txt

******************************


SQLite is in the public domain as described here:

http://www.sqlite.org/copyright.html

SQLite binaries are linked into the NServiceBus distribution allowed under the license terms described above.

******************************


Rhino Mocks is licensed under the BSD License as described here:

http://www.ayende.com/projects/rhino-mocks.aspx

Rhino Mocks binaries are merged into NServiceBus allowed under the license terms described here:

http://www.opensource.org/licenses/bsd-license.php


