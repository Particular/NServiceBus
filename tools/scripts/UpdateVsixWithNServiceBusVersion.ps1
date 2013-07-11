#   $ProductVersion = "3.2.3"
#   $VsixFilePath   = "C:\Users\WorkUser\Desktop\NServiceBusStudio.vsix"
#   $OutputDirectory = "d:\temp"
#   Usage: .\UpdateVsixWithNServiceBusVersion.ps1 3.2.1 d:\nsb\NServiceBusStudio\NServiceBusStudio.vsix d:\temp
param(   
        [parameter(mandatory=$true, position=0)][string]$ProductVersion, 
        [parameter(mandatory=$true, position=1)][string]$VsixFilePath,
        [parameter(mandatory=$true, position=2)][string]$OutputDirectory
    )

    $NServiceBusVersionFile = "NServiceBusVersion.txt"
    $zipOutputTempDirectory = $env:temp + "\" + "$(Get-Date -format 'yyyy_MM_dd_hh_mm_ss')"
    $baseDir  = resolve-path ..
    $zipExec = "$baseDir\zip\7za.exe"

function Rename-FileExtension([string]$filename, [string]$oldExtension, [string]$newExtension)
{
    $baseName = $filename.remove($filename.length - $oldExtension.length, $oldExtension.length)
    Rename-Item $filename ($baseName + $newExtension)
    return [string]($baseName + $newExtension)
}
function RenameExtension([string]$filename, [string]$oldExtension, [string]$newExtension)
{
    $baseName = $filename.remove($filename.length - $oldExtension.length, $oldExtension.length)
    return [string]($baseName + $newExtension)
}

function GetFileNameFromFullPath([string]$fullPathName) 
{
    $fileItem = Get-ChildItem $fullPathName
    return $fileItem.Name
}
# Archive the extracted vsix files and move to file to OutputDirectory
function CreateVsix()
{
    $fileName = GetFileNameFromFullPath $VsixFilePath
    $fileName = RenameExtension $fileName "vsix" "zip"
    $newZipFile = $OutputDirectory +  "\" + $fileName
    Write-Host "newZipFile: " $newZipFile
    $zipOutputTempDirectory = $zipOutputTempDirectory + "\*"
    Write-Host "zipOutputTempDirectory: " $zipOutputTempDirectory
    $arguments =  "a", "-tzip", $newZipFile, $zipOutputTempDirectory;
    &$zipExec $arguments
    $fileName = RenameExtension $newZipFile "zip" "vsix"
    if((Test-Path $filename) -eq $true) 
    {
        remove-item $fileName -Force
    }
    Rename-FileExtension $newZipFile "zip" "vsix"
    remove-item $zipOutputTempDirectory -Force -Recurse
}

# UnZip VSIX content to temp folder.
function Extract()
{
    if((Test-Path $VsixFilePath) -eq $false) 
    {
        Write-Host $VsixFilePath "was not found. Exiting."
        exit 
    }
    $global:zipOutputTempDirectory = $env:temp + "\" + "$(Get-Date -format 'yyyy_MM_dd_hh_mm_ss')"
    Write-Host "Trying to create directory: " + $zipOutputTempDirectory
    New-Item $zipOutputTempDirectory -type directory
    Copy-Item $VsixFilePath $zipOutputTempDirectory
    $newFileLocation = GetFileNameFromFullPath $VsixFilePath
    $newFileLocation = $zipOutputTempDirectory + "\" + $newFileLocation 
    $zipFile = Rename-FileExtension $newFileLocation "vsix" "zip"
    & $zipExec "x", $zipFile, "-o$zipOutputTempDirectory"
    remove-item $zipFile -Force
}


function ChangeNServiceBusVersion
{
    $NServiceBusVersionFile = ($zipOutputTempDirectory + "\" + $NServiceBusVersionFile)
    $nsbVerFile = new-object System.IO.StreamWriter($NServiceBusVersionFile, $false, [System.Text.Encoding]::UTF8)
    $nsbVerion = $ProductVersion
    $nsbVerFile.Write($nsbVerion)
    $nsbVerFile.flush()
    $nsbVerFile.Close()
}

# MAIN PROGRAM #
Extract
ChangeNServiceBusVersion
CreateVsix


