if not exist Download\package\lib\net35 mkdir Download\package\lib\net35
if not exist Download\package\lib\net35-client mkdir Download\package\lib\net35-client

tools\ilmerge.exe /lib:RestSharp\bin\Release /internalize /ndebug /v2 /out:Download\RestSharp.dll RestSharp.dll Newtonsoft.Json.dll
copy RestSharp\bin\Release\RestSharp.xml Download\

copy LICENSE.txt Download

copy RestSharp\bin\Release\RestSharp.dll Download\Package\lib\net35\
copy RestSharp\bin\Release\RestSharp.dll Download\Package\lib\net35-client\

copy RestSharp\bin\Release\RestSharp.xml Download\Package\lib\net35\
copy RestSharp\bin\Release\RestSharp.xml Download\Package\lib\net35-client\

tools\nuget.exe pack restsharp.nuspec -BasePath Download\Package -Output Download