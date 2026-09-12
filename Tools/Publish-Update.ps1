param(
    [Parameter(Mandatory = $true)]
    [string]$Summary,

    [string[]]$Changes = @()
)

$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"
$logPath = Join-Path $projectRoot "CHANGELOG.md"
$logEntry = @(
    "",
    "## $timestamp · $Summary",
    "",
    "- 更新摘要：$Summary"
)

foreach ($change in $Changes) {
    $logEntry += "- $change"
}

Add-Content -LiteralPath $logPath -Value ($logEntry -join [Environment]::NewLine) -Encoding UTF8
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
