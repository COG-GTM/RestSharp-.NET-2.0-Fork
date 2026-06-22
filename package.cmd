if not exist Download\package\lib\net40 mkdir Download\package\lib\net40

tools\ilmerge.exe /lib:RestSharp\bin\Release /internalize /ndebug /v4 /out:Download\RestSharp.dll RestSharp.dll Newtonsoft.Json.dll
copy RestSharp\bin\Release\RestSharp.xml Download\

copy LICENSE.txt Download

copy RestSharp\bin\Release\RestSharp.dll Download\Package\lib\net40\
copy RestSharp\bin\Release\RestSharp.xml Download\Package\lib\net40\

tools\nuget.exe pack restsharp.nuspec -BasePath Download\Package -Output Download
