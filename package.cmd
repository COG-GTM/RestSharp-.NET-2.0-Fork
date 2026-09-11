if not exist Download\package\lib\net8.0 mkdir Download\package\lib\net8.0
if not exist Download\package\lib\netstandard2.0 mkdir Download\package\lib\netstandard2.0

copy RestSharp\bin\Release\net8.0\RestSharp.dll Download\Package\lib\net8.0\
copy RestSharp\bin\Release\net8.0\RestSharp.xml Download\Package\lib\net8.0\
copy RestSharp\bin\Release\netstandard2.0\RestSharp.dll Download\Package\lib\netstandard2.0\
copy RestSharp\bin\Release\netstandard2.0\RestSharp.xml Download\Package\lib\netstandard2.0\

copy LICENSE.txt Download

tools\nuget.exe pack restsharp.nuspec -BasePath Download\Package -Output Download