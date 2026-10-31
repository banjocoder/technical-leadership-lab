param(
  [Parameter(Mandatory)] [string] $BaseUrl,
  [Parameter(Mandatory)] [string] $TargetEnvironment,
  [Parameter(Mandatory)] [string] $OutFile,
  [ValidateSet('Succeeded','Failed')] [string] $ExecutionResult = 'Succeeded',
  [string] $FailureReason = ''
)
$ErrorActionPreference = 'Stop'

if ($ExecutionResult -eq 'Succeeded') {
  $resp     = Invoke-WebRequest "$BaseUrl/readiness" -SkipHttpErrorCheck
  $status   = [int]$resp.StatusCode
  $body     = $resp.Content | ConvertFrom-Json
  $outcome  = $body.status
  $evidence = $body
} else {
  $status   = $null
  $outcome  = 'NotEvaluated'     # <-- NOT "NotReady" — see note below
  $evidence = $null
}

$record = [ordered]@{
  artifact = [ordered]@{
    id           = $env:ARTIFACT_ID
    version      = $env:ARTIFACT_VERSION
    commit       = $env:ARTIFACT_COMMIT
    branch       = $env:ARTIFACT_BRANCH
    buildTimeUtc = $env:ARTIFACT_BUILD_TIME
  }
  deployment = [ordered]@{
    targetEnvironment = $TargetEnvironment
    deployTimeUtc     = (Get-Date).ToUniversalTime().ToString('o')
    executionResult   = $ExecutionResult
    failureReason     = if ($FailureReason) { $FailureReason } else { $null }
    runId             = $env:GITHUB_RUN_ID
  }
  validation = [ordered]@{
    endpoint      = '/readiness'
    httpStatus    = $status
    outcome       = $outcome
    capturedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    evidence      = $evidence
  }
}

New-Item -ItemType Directory -Force -Path (Split-Path $OutFile) | Out-Null
$record | ConvertTo-Json -Depth 10 | Set-Content -Path $OutFile -Encoding utf8

"outcome=$outcome"                 | Out-File $env:GITHUB_OUTPUT -Append
"httpStatus=$status"               | Out-File $env:GITHUB_OUTPUT -Append
"executionResult=$ExecutionResult" | Out-File $env:GITHUB_OUTPUT -Append