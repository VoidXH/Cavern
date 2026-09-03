for /d /r %%I in (.vs)         do @if exist "%%I" rd /s /q "%%I"
for /d /r %%I in (bin)         do @if exist "%%I" rd /s /q "%%I"
for /d /r %%I in (obj)         do @if exist "%%I" rd /s /q "%%I"
for /d /r %%I in (TestResults) do @if exist "%%I" rd /s /q "%%I"
pause
