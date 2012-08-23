foreach ($ext in @("*.cs", "*.txt", "*.bat", "*.sln"))  {
	(dir -Recurse -Filter $ext) | foreach { 
		$file = gc $_.FullName
		$file | Out-File -Encoding UTF8 $_.FullName
	}
	
}