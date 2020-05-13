FROM kwoth/nadeko-service-base:latest

ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

# copy csproj so that i can restore, in order to avoid
# restore when the container is running
COPY "searchimages/SearchImages.csproj" "/app/searchimages/SearchImages.csproj"
COPY "searchimages-service/SearchImagesService.csproj" "/app/searchimages-service/SearchImagesService.csproj"
COPY "nuget.config" "/app/nuget.config"

WORKDIR "/app/searchimages"
RUN dotnet restore

WORKDIR "/app/searchimages-service"
RUN dotnet restore

EXPOSE 2452

ENTRYPOINT dotnet watch run
