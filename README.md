Kobush.Build
============

Xml logger for msbuild

## Nuget 

Nuget package http://nuget.org/packages/Kobush.Build/

To Install from the Nuget Package Manager Console 
    
    PM> Install-Package Kobush.Build
    
## Summary

To use additional logger with MSBuild you use the /logger switch. It requires the fully qualified class name of the logger (including namespace) and assembly name separated with coma. You can also pass additional parameters to the logger after a semicolon. My logger takes only one parameter, and it is the output filename. If this parameter is opmitted output will be directed to console.

To use the XmlLogger when building your project execute following command line:

    MSBuild.exe yourproject /logger:Kobush.Build.Logging.XmlLogger,Kobush.MSBuild.dll;buildresult.xml

Where yourproject can be a Visual Studio solution file (.sln), a language specific project file (.csproj or .vbproj) or a custom build file.

You can also add following switches to stop all output to the console:

    /nologo /noconsolelogger

## Icon

Code by Luboš Volkov from The [The Noun Project](http://thenounproject.com)

## Orginal Page

This project is extension of orginal project and move it on github and nuget:
http://geekswithblogs.net/kobush/archive/2006/01/14/xmllogger.aspx
