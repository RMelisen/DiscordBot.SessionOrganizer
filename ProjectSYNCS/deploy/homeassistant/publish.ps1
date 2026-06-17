$output = "$PSScriptRoot\addons\projectsyncs\app"

Write-Host "Building self-contained ARM64 binary..."
dotnet publish "$PSScriptRoot\..\..\ProjectSYNCS.csproj" `
    -c Release `
    -r linux-arm64 `
    --self-contained true `
    -o $output

Write-Host ""
Write-Host "Done. Copy the folder below to your Pi via Samba:"
Write-Host "  Source : $PSScriptRoot\addons\projectsyncs\"
Write-Host "  Target : \\<HA-IP>\addons\projectsyncs\"
