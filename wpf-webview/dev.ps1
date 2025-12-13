# Start Vite dev server
# Vite will automatically write the URL to .vite-dev-server file via vite.config.js
$originalLocation = Get-Location
try {
    Set-Location "$PSScriptRoot\src\WpfWebview.Web"
    pnpm dev
}
finally {
    Set-Location $originalLocation
}
