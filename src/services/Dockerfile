FROM kwoth/nadeko-service-base

EXPOSE 2452

# Before building this docker image, you have to run
# dotnet build -c Release -o bin/docker_output/ -r debian.10-x64 -p:DefineConstants=DOCKER_ENVIRONMENT --force
ADD bin/docker_output /app/

WORKDIR /app
CMD ["dotnet", "/app/service.dll"]
