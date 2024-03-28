@echo Sleeping
@powershell -command "Write-Output 'output!'; Write-Error 'error!'"
@echo Awake!