$ErrorActionPreference = "Stop"

$check = [char]0x2713
$cross = [char]0x2717

if ($args.Length -ne 1) {
    Write-Host "Invalid number of arguments" -ForegroundColor Red
    exit(-1)
}

if ($args[0] -eq "" -or !(Test-Path $args[0])) {
    Write-Host "File '$($args[0])' does not exist" -ForegroundColor Red
    exit(-1)
}

$manifest_content = Get-Content $args[0] | ConvertFrom-Json

$manifest_version_str = $manifest_content.version
$semver = [regex]::Match($manifest_version_str, '^(?<prerelease>(?<version>(?:0|[1-9]\d*)\.(?:0|[1-9]\d*)\.(?:0|[1-9]\d*))(?:-(?:(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?)(?:\+(?:[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$')

$version_with_prerelease = $semver.groups['prerelease'].value

$git_hash = (git log -n 1 --pretty=%h).Trim()
$git_tag = git tag -l --points-at HEAD

if ($git_tag -ne "" -and $git_tag.Length -gt 0) {
    if ($git_tag -ne "v$version_with_prerelease") {
        Write-Host "$cross Git tag '$git_tag' does not match manifest version '$version_with_prerelease'" -ForegroundColor Red
        exit(-1)
    }

    Write-Host "$check Using Git tag '$git_tag'" -ForegroundColor Green
    $manifest_content.version = $version_with_prerelease
    $zip_version = "v$version_with_prerelease"
} elseif ($git_hash -ne "" -and $git_hash.Length -gt 0) {
    Write-Host "$check Using Git hash '$git_hash'" -ForegroundColor Green
    $manifest_content.version = "$version_with_prerelease+git.$git_hash"
    $zip_version = $git_hash
} else {
    Write-Host "$cross Could not find Git tag or hash" -ForegroundColor Red
    exit(-1)
}

Add-Content "$env:GITHUB_ENV" "ZIP_VERSION=$zip_version"
Add-Content "$env:GITHUB_ENV" "GAME_VERSION=$($manifest_content.gameVersion)"

$manifest_content | ConvertTo-Json | Set-Content $args[0]
