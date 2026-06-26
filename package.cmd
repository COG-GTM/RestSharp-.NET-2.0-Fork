if not exist Download\package\lib\net48 mkdir Download\package\lib\net48

copy RestSharp\bin\Release\RestSharp.dll Download\
copy RestSharp\bin\Release\RestSharp.xml Download\

copy LICENSE.txt Download

copy RestSharp\bin\Release\RestSharp.dll Download\Package\lib\net48\
copy RestSharp\bin\Release\RestSharp.xml Download\Package\lib\net48\

tools\nuget.exe pack restsharp.nuspec -BasePath Download\Package -Output Download
