#Usage: .\UploadToGithub.ps1 "githubuser:password" C:\path to file\X.zip

param($credentials,$path)
$fileToUpload = get-item $path

$uploadMetadata = [pscustomobject]@{name=$fileToUpload.name; size=$fileToUpload.length;}

$json = ConvertTo-Json $uploadMetadata -Compress

$json = $json -replace '"' , '"""'
#C:\Users\andreas.ohlund\Documents\WindowsPowerShell\Modules\Pscx\Apps\echoargs.exe
$result = & curl  --user $credentials -d $json "https://api.github.com/repos/nservicebus/nservicebus/downloads"

$metaData = ConvertFrom-Json "$result"

if($metaData.errors -ne $null)
{
    Write-Host "Failed: " $metaData.message
    exit
}
Write-Host "Metadata created, going to upload file"

Write-Host $result

$result = & curl -F "key=$($metaData.path)" -F "acl=$($metaData.acl)" -F "success_action_status=201" -F "Filename=$($metaData.name)" -F "AWSAccessKeyId=$($metaData.accesskeyid)" -F "Policy=$($metaData.policy)" -F "Signature=$($metaData.signature)" -F "Content-Type=$($metaData.mime_type)" -F "file=@$path" https://github.s3.amazonaws.com/

Write-Host $result