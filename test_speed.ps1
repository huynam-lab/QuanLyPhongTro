$headers = @{
    "Authorization" = "Bearer sk-00bc0e596617a9410b5885f2807af73c350f08e29ca5041169815536d6cf7fd1"
    "Content-Type"  = "application/json"
}

$body = '{ "model": "deepseek-v4-flash", "messages": [{"role": "user", "content": "Xin chao"}], "max_tokens": 50 }'

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$res = Invoke-RestMethod -Uri "https://api.vilao.ai/v1/chat/completions" -Method Post -Headers $headers -Body $body
$sw.Stop()

Write-Host "Total Time: $($sw.ElapsedMilliseconds) ms"
Write-Host "Response: $($res.choices[0].message.content)"
