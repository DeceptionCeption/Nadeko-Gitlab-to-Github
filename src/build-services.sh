build_svc() {
  pushd "services/$svc/service"
  {
    rm -rf ./docker_output
    dotnet build -c Release -o bin/docker_output/ -r debian.10-x64 -p:DOCKER_ENVIRONMENT=1 --force
    rm -rf bin/docker_output/config
    rm -rf bin/docker_output/data
    cp ../../../wait-for-it.sh ./wait-for-it.sh
    {
      docker build -f ../../Dockerfile . --no-cache -t "$1-service:latest"
    }
    rm wait-for-it.sh
  }
  popd
}

[[ "$#" -eq 0 ]] && services=("expressions" "searchimages") || services=$@
for svc in ${services[@]};
do
  echo "$svc build starting..."
  build_svc $svc
  echo "$svc build ended"
done
