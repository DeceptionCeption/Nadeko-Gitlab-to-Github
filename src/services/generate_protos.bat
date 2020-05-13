setlocal

cd /d %~dp0

set PROTOC=protoc.exe
set PLUGIN=grpc_csharp_plugin.exe

%PROTOC% -I _protos --csharp_out Searches/searches  _protos/searches.proto --grpc_out Searches/searches --plugin=protoc-gen-grpc=%PLUGIN%
%PROTOC% -I _protos --csharp_out Localization/localization  _protos/localization.proto --grpc_out Localization/localization --plugin=protoc-gen-grpc=%PLUGIN%
%PROTOC% -I _protos --csharp_out BotConfig/botconfig  _protos/botconfig.proto --grpc_out BotConfig/botconfig --plugin=protoc-gen-grpc=%PLUGIN%

endlocal
