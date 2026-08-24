param($filepath)
(Get-Content $filepath) -replace '^pick 8734928', 'edit 8734928' | Set-Content $filepath
