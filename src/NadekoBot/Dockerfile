FROM kwoth/v3base:latest
# Before building this docker image, in Ayu/Nadeko.Coordinator, you have to run
# dotnet build -c Release -r alpine.3.10-x64 -o linux_output/
COPY bin/linux_output/ /app/
# make executable runnable
RUN chmod +x /app/NadekoBot

WORKDIR /app
CMD ["dotnet", "exec /app/NadekoBot.dll"]
