call "%VS90COMNTOOLS%vsvars32.bat"
clean
build_src
merge_with_strong_name
build_tools
build_samples