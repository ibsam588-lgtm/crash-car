param(
    [Parameter(Mandatory=$true)][string]$UnityEditor,
    [Parameter(Mandatory=$true)][string]$AndroidSdk,
    [Parameter(Mandatory=$true)][string]$AndroidNdk,
    [Parameter(Mandatory=$true)][string]$Jdk,
    [Parameter(Mandatory=$true)][ValidateRange(292,99999)][int]$VersionCode,
    [string]$VersionName='1.1.0'
)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location -LiteralPath $repo
foreach($toolPath in @($UnityEditor,$AndroidSdk,$AndroidNdk,$Jdk)) {
    if(-not (Test-Path -LiteralPath $toolPath)){throw "Missing Android tool: $toolPath"}
}
if($VersionName -notmatch '^\d+\.\d+\.\d+(?:[.-][A-Za-z0-9]+)*$'){throw 'Use a semantic version name.'}
$dirty=git status --porcelain --untracked-files=normal
if($LASTEXITCODE -ne 0 -or $dirty){throw 'Commit the source changes before exporting a release candidate.'}
$commit=(git rev-parse HEAD).Trim()
if($LASTEXITCODE -ne 0){throw 'Cannot read the source commit.'}
$tree=(git rev-parse 'HEAD:unity/CarCrashUnityPrototype').Trim()
if($LASTEXITCODE -ne 0){throw 'Cannot read the Unity source tree.'}
$buildStamp=Get-Date -Format 'yyyyMMdd-HHmmss'
# Unity's Android CMake dependencies gain long generated paths below the project.
# Keep the directory short and record the full source commit in the receipt.
$buildRoot=Join-Path $repo "build/android-$VersionCode-$buildStamp"
$project=Join-Path $buildRoot 'project'
if(Test-Path -LiteralPath $buildRoot){throw "Build directory already exists. Inspect it before retrying: $buildRoot"}
New-Item -ItemType Directory -Path $project -Force | Out-Null
$archive=Join-Path $buildRoot 'project.tar'
git archive --format=tar --output=$archive HEAD unity/CarCrashUnityPrototype
if($LASTEXITCODE -ne 0){throw 'Cannot archive the committed Unity project.'}
& tar -xf $archive -C $project --strip-components=2
if($LASTEXITCODE -ne 0){throw 'Cannot extract the committed Unity project.'}
$env:CRASH_SOURCE_COMMIT=$commit
$env:CRASH_SOURCE_TREE=$tree
$env:CRASH_VERSION_CODE=$VersionCode.ToString()
$env:CRASH_VERSION_NAME=$VersionName
$env:CRASH_ANDROID_SDK=$AndroidSdk
$env:CRASH_ANDROID_NDK=$AndroidNdk
$env:CRASH_ANDROID_JDK=$Jdk
$env:CRASH_NATIVE_AAB=Join-Path $buildRoot 'CrashCar-cloud.aab'
$log=Join-Path $buildRoot 'unity-android.log'
$arguments=@('-batchmode','-nographics','-quit','-buildTarget','Android','-projectPath',('"'+$project+'"'),'-executeMethod','CarCrashEditor.AndroidReleaseBuilder.BuildForGitHubSigning','-logFile',('"'+$log+'"'))
Write-Output "Building committed Android source $commit with Unity $UnityEditor"
Write-Output "Build log: $log"
$process=Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $buildRoot 'stdout.log') -RedirectStandardError (Join-Path $buildRoot 'stderr.log')
# Gradle can keep a background daemon alive after Unity has finished the export.
$process.WaitForExit()
if($process.ExitCode -ne 0){throw "Unity Android export failed ($($process.ExitCode)). Inspect $log"}
$receipt=Join-Path $buildRoot 'native-source.json'
if(-not (Test-Path -LiteralPath $receipt)){throw 'Unity did not produce a native build receipt.'}
Write-Output "Native candidate: $env:CRASH_NATIVE_AAB"
Write-Output "Source receipt: $receipt"
