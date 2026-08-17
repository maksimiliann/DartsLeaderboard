param(
    [int]$Port = 5599,
    [string]$Root = "$PSScriptRoot"
)

$listener = [System.Net.HttpListener]::new()
$listener.Prefixes.Add("http://localhost:$Port/")
$listener.Start()
Write-Host "serving $Root on http://localhost:$Port/"

try {
    while ($listener.IsListening) {
        $context = $listener.GetContext()
        $path = $context.Request.Url.LocalPath.TrimStart('/')
        if ([string]::IsNullOrWhiteSpace($path)) { $path = 'layout.html' }
        $file = Join-Path $Root $path

        if (Test-Path -LiteralPath $file -PathType Leaf) {
            $bytes = [System.IO.File]::ReadAllBytes($file)
            $context.Response.ContentType = 'text/html; charset=utf-8'
            $context.Response.StatusCode = 200
            $context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
        } else {
            $context.Response.StatusCode = 404
        }
        $context.Response.Close()
    }
} finally {
    $listener.Stop()
}
