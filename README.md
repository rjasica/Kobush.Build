Kobush.Build
============

Xml logger for msbuild

To use additional logger with MSBuild you use the /logger switch. It requires the fully qualified class name of the logger (including namespace) and assembly name separated with coma. You can also pass additional parameters to the logger after a semicolon. My logger takes only one parameter, and it is the output filename. If this parameter is opmitted output will be directed to console.

To use the XmlLogger when building your project execute following command line:

    MSBuild.exe yourproject /logger:Kobush.Build.Logging.XmlLogger,Kobush.MSBuild.dll;buildresult.xml

Where yourproject can be a Visual Studio solution file (.sln), a language specific project file (.csproj or .vbproj) or a custom build file.

You can also add following switches to stop all output to the console:

    /nologo /noconsolelogger
