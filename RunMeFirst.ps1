$runnerExec = ""
if(Test-Path  ".\build.ps1"){
	.\build.ps1	
	$runnerExec = ".\build\tools\MsmqUtils\runner.exe" 
   
}
else if(Test-Path ".\tools\msmqutils\runner.exe") {
	
	$runnerExec = ".\tools\msmqutils\runner.exe" 
}

if($runnerExec -ne ""){
exec{ &$runnerExec $args }
}
