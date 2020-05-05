Remove-Item .\_nupkg -Force -Recurse -ErrorAction Ignore;
dotnet pack -c Release -o _nupkg
..\..\..\nuget.exe push .\_nupkg\*.nupkg "$env:myget_api_key" -source  https://www.myget.org/F/kwoth/api/v2/package
Remove-Item .\_nupkg -Force -Recurse -ErrorAction Ignore;
