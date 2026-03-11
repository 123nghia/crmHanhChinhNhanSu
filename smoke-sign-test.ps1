$ErrorActionPreference = 'Stop'

function Get-Token([string]$Html) {
    $m = [regex]::Match($Html, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"')
    if (-not $m.Success) { throw 'Cannot find antiforgery token' }
    return $m.Groups[1].Value
}

function Login([string]$BaseUrl, [string]$UserName, [string]$Password) {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$BaseUrl/Login" -WebSession $session -MaximumRedirection 5
    $token = Get-Token $loginPage.Content
    try {
        Invoke-WebRequest -Uri "$BaseUrl/Login?handler=Login" -Method Post -WebSession $session -MaximumRedirection 0 -ContentType 'application/x-www-form-urlencoded' -Headers @{ 'RequestVerificationToken' = $token } -Body @{ '__RequestVerificationToken' = $token; 'UserName' = $UserName; 'Password' = $Password } | Out-Null
    }
    catch {
        if (-not $_.Exception.Response) { throw }
    }

    $homePage = Invoke-WebRequest -Uri "$BaseUrl/" -WebSession $session -MaximumRedirection 5
    if ($homePage.BaseResponse.ResponseUri.AbsoluteUri -match '/Login') { throw "Login failed for $UserName" }
    return $session
}

$baseUrl = 'http://localhost:5232'
$workspace = 'c:/Users/Nghia/Desktop/temp/crmHanhChinhNhanSu'
$adminUser = 'admin'
$adminPass = '123'
$employeeUser = 'huynh.bich'
$employeePass = 'Vietstar@2026'

Write-Host '1) Login as admin'
$adminSession = Login -BaseUrl $baseUrl -UserName $adminUser -Password $adminPass

Write-Host '2) Find employee row for huynh.bich'
$employeePage = Invoke-WebRequest -Uri "$baseUrl/Employee" -WebSession $adminSession -MaximumRedirection 5
$employeeContent = $employeePage.Content
$employeeId = $null
$employeeFullName = $null
$employeePattern = '(?s)<tr\s+data-id="(?<id>\d+)">.*?<td>\s*' + [regex]::Escape($employeeUser) + '\s*</td>.*?<a href="/EmployeeInfo\?id=\d+">(?<fullname>.*?)</a>'
$employeeMatch = [regex]::Match($employeeContent, $employeePattern)
if ($employeeMatch.Success) {
    $employeeId = [int]$employeeMatch.Groups['id'].Value
    $employeeFullName = $employeeMatch.Groups['fullname'].Value.Trim()
}
if (-not $employeeId) { throw 'Could not find employee huynh.bich on /Employee page' }
Write-Host ("   EmployeeId={0}; FullName={1}" -f $employeeId, $employeeFullName)

Write-Host '3) Open employee info and pick a signable document'
$employeeInfo = Invoke-WebRequest -Uri "$baseUrl/EmployeeInfo?id=$employeeId" -WebSession $adminSession -MaximumRedirection 5
$employeeInfoHtml = $employeeInfo.Content
$employeeInfoToken = Get-Token $employeeInfoHtml
$docMatches = [regex]::Matches($employeeInfoHtml, 'requestEmployeeDocumentSign\((\d+)\)')
if ($docMatches.Count -lt 1) { throw 'No sign-request button found on employee documents' }
$documentId = [int]$docMatches[0].Groups[1].Value
Write-Host ("   Chosen documentId={0}" -f $documentId)

Write-Host '4) Request signature as admin'
$requestResp = Invoke-WebRequest -Uri "$baseUrl/EmployeeInfo?handler=RequestDocumentSign&id=$documentId" -Method Post -WebSession $adminSession -Headers @{ 'RequestVerificationToken' = $employeeInfoToken } -MaximumRedirection 5
$requestJson = $requestResp.Content | ConvertFrom-Json
if (-not $requestJson.success) { throw 'RequestDocumentSign did not return success' }
Write-Host ("   Requested at {0} by {1}" -f $requestJson.requestedAt, $requestJson.requestedBy)

Write-Host '5) Login as employee'
$employeeSession = Login -BaseUrl $baseUrl -UserName $employeeUser -Password $employeePass

Write-Host '6) Open self EmployeeInfo and fetch sign form'
$selfInfo = Invoke-WebRequest -Uri "$baseUrl/EmployeeInfo?id=$employeeId" -WebSession $employeeSession -MaximumRedirection 5
$selfHtml = $selfInfo.Content
$selfToken = Get-Token $selfHtml
$signForm = Invoke-WebRequest -Uri "$baseUrl/EmployeeInfo?handler=DocumentSignForm&id=$documentId" -WebSession $employeeSession -MaximumRedirection 5
if ($signForm.Content -notmatch 'Ky xac nhan tai lieu') { throw 'Sign form did not load' }
if ($signForm.Content -notmatch 'employeeSignatureCanvas') { throw 'Sign form missing signature canvas' }
Write-Host '   Sign form loaded successfully'

Write-Host '7) Sign document as employee'
$signaturePngBase64 = 'iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAFElEQVQImWP8//8/AwMDEwMDAwMjAAB5AQMDCYXZ4QAAAABJRU5ErkJggg=='
$signatureDataUrl = 'data:image/png;base64,' + $signaturePngBase64
$signResp = Invoke-WebRequest -Uri "$baseUrl/EmployeeInfo?handler=SignDocumentInternal" -Method Post -WebSession $employeeSession -Headers @{ 'RequestVerificationToken' = $selfToken } -ContentType 'application/x-www-form-urlencoded' -Body @{ 'Id' = $documentId; 'PasswordConfirm' = $employeePass; 'SignatureCode' = 'SMOKE_TEST'; 'SignNote' = 'Automated smoke test'; 'AcceptTerms' = 'true'; 'SignatureDataUrl' = $signatureDataUrl } -MaximumRedirection 5
$signJson = $signResp.Content | ConvertFrom-Json
if (-not $signJson.success) { throw 'SignDocumentInternal did not return success' }
Write-Host ("   Signed at {0} by {1}" -f $signJson.signedAt, $signJson.signedBy)

Write-Host '8) Verify signed artifacts on self page'
$verifyPage = Invoke-WebRequest -Uri "$baseUrl/EmployeeInfo?id=$employeeId" -WebSession $employeeSession -MaximumRedirection 5
$verifyHtml = $verifyPage.Content
if ($verifyHtml -notmatch 'Da ky noi bo') { throw 'Signed status text not found on verify page' }
$archiveMatch = [regex]::Match($verifyHtml, 'href="(?<path>/signed-archive/employee-documents/[^"]+)"[^>]*>Mo file da ky</a>')
$imageMatch = [regex]::Match($verifyHtml, 'href="(?<path>/signed-archive/employee-signatures/[^"]+)"[^>]*>Mo anh chu ky</a>')
if (-not $archiveMatch.Success) { throw 'Signed archive link not found' }
if (-not $imageMatch.Success) { throw 'Signature image link not found' }
$archiveRel = $archiveMatch.Groups['path'].Value
$imageRel = $imageMatch.Groups['path'].Value
$archiveFs = Join-Path $workspace ('crmHuman/wwwroot' + ($archiveRel -replace '/', '\\'))
$imageFs = Join-Path $workspace ('crmHuman/wwwroot' + ($imageRel -replace '/', '\\'))
$archiveExists = Test-Path $archiveFs
$imageExists = Test-Path $imageFs

[pscustomobject]@{
    EmployeeId = $employeeId
    EmployeeUser = $employeeUser
    DocumentId = $documentId
    RequestedAt = $requestJson.requestedAt
    RequestedBy = $requestJson.requestedBy
    SignedAt = $signJson.signedAt
    SignedBy = $signJson.signedBy
    ArchiveRelativePath = $archiveRel
    ArchiveExists = $archiveExists
    SignatureImageRelativePath = $imageRel
    SignatureImageExists = $imageExists
} | Format-List
