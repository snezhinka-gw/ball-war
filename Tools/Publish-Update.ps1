param(
    [Parameter(Mandatory = $true)]
    [string]$Summary
)

$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot
git add --all

$stagedFiles = @(git diff --cached --name-only)
if ($stagedFiles.Count -eq 0) {
    Write-Host "没有可提交的更新。"
    exit 0
}

git commit -m "Update: $Summary"
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$origin = git remote get-url origin 2>$null
if ([string]::IsNullOrWhiteSpace(($origin -join ""))) {
    Write-Host "本地提交已完成，但尚未配置 origin 远程仓库。"
    exit 0
}

git push origin HEAD
exit $LASTEXITCODE
