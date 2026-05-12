# Start Vite dev server (port 5174; writes .vite-dev-server for WebView2 Debug navigation)
$originalLocation = Get-Location
try {
    Set-Location "$PSScriptRoot\src\WebViewMarkdownTip.Web"
    pnpm dev
}
finally {
    Set-Location $originalLocation
}
