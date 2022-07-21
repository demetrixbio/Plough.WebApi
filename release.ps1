powershell.exe -Command { 
    $env:GITHUB_TOKEN="<github_token>"
    $Env:NUGET_TOKEN="<nuget_token>" 
    .\fake.cmd build -t Release 
}
