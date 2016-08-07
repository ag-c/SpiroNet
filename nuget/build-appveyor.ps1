$ErrorActionPreference = "Stop"
$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath
Push-Location $dir

sv version $env:APPVEYOR_BUILD_NUMBER
#sv version "1-debug"

sv version 0.5.0-build$version-alpha
sv key $env:myget_key

. ".\include.ps1"
.\build-version.ps1 $version

sv reponame $env:APPVEYOR_REPO_NAME
sv repobranch $env:APPVEYOR_REPO_BRANCH
sv pullreq $env:APPVEYOR_PULL_REQUEST_NUMBER

echo "Checking for publishing"
echo "$reponame $repobranch $pullreq"
if ([string]::IsNullOrWhiteSpace($pullreq))
{
    echo "Build is not a PR"
    if($repobranch -eq "master")
    {
        echo "Repo branch matched"
        foreach($pkg in $Packages)
        {
            nuget.exe push "$($pkg).$($version).nupkg" $key -Source https://www.myget.org/F/spironet-nightly/api/v2/package
        }
    }
}


