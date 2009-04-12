call "%VS90COMNTOOLS%vsvars32.bat"
call "clean"
call "build_src"
xcopy build build\merge /Q /Y
xcopy external-bin build\merge /Q /Y
call "merge_assemblies" /keyfile:NServiceBus.snk
echo NServiceBus.dll merged
move NServiceBus.dll build\output
move NServiceBus.pdb build\output
move NServiceBus.xml build\output
del build\merge\*.* /Q
call "build_tools"
call "build_samples"