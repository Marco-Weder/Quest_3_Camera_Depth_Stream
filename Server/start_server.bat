@echo off

REM Create venv if missing
if not exist .venv (
    python -m venv .venv
)
call .venv\Scripts\activate

REM Install deps
pip install --upgrade pip
pip install numpy pyvirtualcam opencv-python

REM Firewall rule: TCP 5555
netsh advfirewall firewall show rule name="Allow TCP 5555" >nul 2>&1
if %errorlevel% neq 0 (
    netsh advfirewall firewall add rule name="Allow TCP 5555" dir=in action=allow protocol=TCP localport=5555
)

REM Firewall rule: TCP 8888
netsh advfirewall firewall show rule name="Allow TCP 8888" >nul 2>&1
if %errorlevel% neq 0 (
    netsh advfirewall firewall add rule name="Allow TCP 8888" dir=in action=allow protocol=TCP localport=8888
)

REM Firewall rule: UDP 9999
netsh advfirewall firewall show rule name="Allow UDP 9999" >nul 2>&1
if %errorlevel% neq 0 (
    netsh advfirewall firewall add rule name="Allow UDP 9999" dir=in action=allow protocol=UDP localport=9999
)

REM ADB reverse (if device found)
for /f "tokens=1" %%d in ('adb devices ^| findstr /R /C:"device$"') do (
    adb reverse tcp:8888 tcp:8888
)

REM Run receiver
python receiver.py
pause
