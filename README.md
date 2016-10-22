# FileShare Foreman [![Build status](https://ci.appveyor.com/api/projects/status/46jbsj2eh7b2iob9?svg=true)](https://ci.appveyor.com/project/jjcamp/fsforeman)

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