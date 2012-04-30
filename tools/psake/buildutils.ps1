﻿
function Delete-Directory($directoryName){
	Remove-Item -Force -Recurse $directoryName -ErrorAction SilentlyContinue
}
 
function Create-Directory($directoryName){
	New-Item $directoryName -ItemType Directory | Out-Null
}

function Get-RegistryValues($key) {
  (Get-Item $key -ErrorAction SilentlyContinue).GetValueNames()
}

function Get-RegistryValue($key, $value) {
    (Get-ItemProperty $key $value -ErrorAction SilentlyContinue).$value
}

function AddType{
	Add-Type -TypeDefinition "
	using System;
	using System.Runtime.InteropServices;
	public static class Win32Api
	{
	    [DllImport(""Kernel32.dll"", EntryPoint = ""IsWow64Process"")]
	    [return: MarshalAs(UnmanagedType.Bool)]
	    public static extern bool IsWow64Process(
	        [In] IntPtr hProcess,
	        [Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process
	    );
	}
	"
}

function Is64BitOS{
    return (Test-64BitProcess) -or (Test-Wow64)
}

function Is64BitProcess{
    return [IntPtr]::Size -eq 8
}

function IsWow64{
    if ([Environment]::OSVersion.Version.Major -eq 5 -and 
        [Environment]::OSVersion.Version.Major -ge 1 -or 
        [Environment]::OSVersion.Version.Major -ge 6)
    {
		AddType
        $process = [System.Diagnostics.Process]::GetCurrentProcess()
        
        $wow64Process = $false
        
        if ([Win32Api]::IsWow64Process($process.Handle, [ref]$wow64Process) -eq $true)
        {
            return $true
        }
		else
		{
			return $false
		}
    }
    else
    {
        return $false
    }
}
 
 $ilMergeExec = ".\tools\IlMerge\ilmerge.exe"
function Ilmerge($key, $directory, $name, $assemblies, $attributeAssembly, $extension, $ilmergeTargetframework, $logFileName, $excludeFilePath){    
	echo "Merging $name....."	
	
    new-item -path $directory -name "temp_merge" -type directory -ErrorAction SilentlyContinue
	
	if($attributeAssembly -ne ""){
    	&$ilMergeExec /keyfile:$key /out:"$directory\temp_merge\$name.$extension" /log:$logFileName /internalize:$excludeFilePath /attr:$attributeAssembly $ilmergeTargetframework $assemblies
	}
	else{
		&$ilMergeExec /keyfile:$key /out:"$directory\temp_merge\$name.$extension" /log:$logFileName /internalize:$excludeFilePath $ilmergeTargetframework $assemblies
	}
    Get-ChildItem "$directory\temp_merge\**" -Include *.$extension, *.pdb, *.xml | Copy-Item -Destination $directory
    Remove-Item "$directory\temp_merge" -Recurse -ErrorAction SilentlyContinue
	$mergeLogContent = Get-Content "$logFileName"
	echo "------------------------------$name Merge Log-----------------------"
	echo $mergeLogContent
}
 
function Generate-Assembly-Info{

param(
	[string]$assemblyTitle,
	[string]$assemblyDescription,
	[string]$clsCompliant = "true",
	[string]$internalsVisibleTo = "",
	[string]$configuration, 
	[string]$company, 
	[string]$product, 
	[string]$copyright, 
	[string]$version,
	[string]$fileVersion,
	[string]$infoVersion,	
	[string]$file = $(throw "file is a required parameter.")
)
	if($infoVersion -eq ""){
		$infoVersion = $fileVersion
	}
	
	$asmInfo = "using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle(""$assemblyTitle"")]
[assembly: AssemblyDescription(""$assemblyDescription"")]
[assembly: AssemblyVersion(""$version"")]
[assembly: AssemblyFileVersion(""$fileVersion"")]
[assembly: AssemblyCopyright(""$copyright"")]
[assembly: AssemblyProduct(""$product"")]
[assembly: AssemblyCompany(""$company"")]
[assembly: AssemblyConfiguration(""$configuration"")]
[assembly: AssemblyInformationalVersion(""$infoVersion"")]
[assembly: ComVisible(false)]		
"
	
	if($clsCompliant.ToLower() -eq "true"){
		 $asmInfo += "[assembly: CLSCompliantAttribute($clsCompliant)]
"
	} 
	
	if($internalsVisibleTo -ne ""){
		$asmInfo += "[assembly: InternalsVisibleTo(""$internalsVisibleTo"")]
"	
	}
	
	

	$dir = [System.IO.Path]::GetDirectoryName($file)
	
	if ([System.IO.Directory]::Exists($dir) -eq $false)
	{
		Write-Host "Creating directory $dir"
		[System.IO.Directory]::CreateDirectory($dir)
	}
	Write-Host "Generating assembly info file: $file"
	Write-Output $asmInfo > $file
}