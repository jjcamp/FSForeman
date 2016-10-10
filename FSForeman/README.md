# FileShare Foreman

FileShare Foreman (FSF) is a Windows service whose primary job is to look for duplicate
files in a file share.  It does this by performing an MD5 hash on each file, and then
checking for duplicate hashes.

FSF is still very much under development, but with a small amount of hacking could be used
in its current iteration.

## Features
- Consistantly monitors one or more root directories and their children
- Web UI
- REST API
- Ignore directories or files using regular expression patterns

## Major TODOs:
- Currently maxes out at 2.1 trillion files
- Make Web UI pretty
- Add tests