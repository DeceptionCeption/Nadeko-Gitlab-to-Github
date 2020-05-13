rm -r -fo -erroraction Ignore bin/linux_output
dotnet build -c GlobalNadeko -r debian.10-x64 -o bin/linux_output/ --force -p:DefineConstants=DOCKER_ENVIRONMENT
cp -erroraction Ignore ../wait-for-it.sh .
docker-compose build bot