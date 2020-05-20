$PROTOC='../protoc.exe'
$PLUGIN='../grpc_csharp_plugin.exe'

Push-Location $PSScriptRoot

$file = Get-ChildItem *.proto
$serviceName = $file.Directory.Name.ToLower();

# mkdir $serviceName -ErrorAction Ignore;

$cmd = "$($PROTOC) --proto_path . -I ../_protos --csharp_out base $($file.Name) --grpc_out base --plugin=protoc-gen-grpc=$PLUGIN" 

Invoke-Expression $cmd

Pop-Location