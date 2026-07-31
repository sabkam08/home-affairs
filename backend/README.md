# Backend

This backend is a standalone Java HTTP server so it can run without Maven or Gradle in this environment.

## Build

```powershell
$out = 'c:\Users\Sabelo Kama\Documents\VSCode\Next\home-affairs\backend\out'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Path $out | Out-Null
javac -d $out (Get-ChildItem -Path 'c:\Users\Sabelo Kama\Documents\VSCode\Next\home-affairs\backend\src\main\java' -Recurse -Filter *.java).FullName
```

## Run

```powershell
java -cp "c:\Users\Sabelo Kama\Documents\VSCode\Next\home-affairs\backend\out" com.homeaffairs.HomeAffairsApplication 8081
```

## Default Demo Accounts

- `admin@homeaffairs.gov.za` / `ChangeMe123!`
- `reviewer@homeaffairs.gov.za` / `ChangeMe123!`
- `employee@homeaffairs.gov.za` / `ChangeMe123!`
- `citizen@example.com` / `ChangeMe123!`
