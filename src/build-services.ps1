# function build-service($svcName){
#     $ErrorActionPreference = 'Ignore'
#     # clear previous build
#     rm -r -fo ./docker_output

#     $ErrorActionPreference = 'Stop'
#     # build 
#     dotnet build -c Release -o bin/docker_output/ -r debian.10-x64 -p:DefineConstants=DOCKER_ENVIRONMENT --force

#     $ErrorActionPreference = 'Ignore'
#     # remove config to prevent conflict
#     rm -r -fo bin/docker_output/config
#     # remove data to prevent conflict
#     rm -r -fo bin/docker_output/data
#     rm -r -fo bin/docker_output/*.pdb

#     $ErrorActionPreference = 'Stop'
#     docker build . --no-cache -t "${svcName}-service:latest"
# }

# function move_and_build($svcName)
# {
#     Push-Location services/$svcName/service
#     try {
#         build-service($svcName)
#     }
#     finally {
#         Pop-Location
#     }
# }


$services = if($args.count -eq 0) {"expressions", "searchimages"}
    else {$args}
# gotta build common first, otherwise all services will try to build it at once since they all 
# depend on it, and fail because files will be in use.

# $services = "xp", "expressions"
$services | ForEach-Object -Throttle 10 -Parallel {
    $svcName = $_
    Push-Location services/$svcName/service
    try 
    {
        $ErrorActionPreference = 'Ignore'
        # clear previous build
        rm -r -fo ./docker_output

        $ErrorActionPreference = 'Stop'
        # build 
        dotnet build -c Release -o bin/docker_output/ -r debian.10-x64 -p:DOCKER_ENVIRONMENT=1 -p:DefineConstants=DOCKER_ENVIRONMENT --force

        $ErrorActionPreference = 'Ignore'
        # remove config to prevent conflict
        rm -r -fo bin/docker_output/config
        # remove data to prevent conflict
        rm -r -fo bin/docker_output/data
        # rm -r -fo bin/docker_output/*.pdb

        $ErrorActionPreference = 'Stop'
        cp ../../../wait-for-it.sh ./wait-for-it.sh
        try{
        docker build -f ../../Dockerfile . --no-cache -t "${svcName}-service:latest"
        }
        finally{
            rm wait-for-it.sh
        }
    }
    finally 
    {
        Pop-Location
    }
}