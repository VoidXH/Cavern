cd Test.Cavern
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=..\TestResults\coverage
reportgenerator -reports:..\TestResults\coverage.cobertura.xml -targetdir:..\TestResults\HtmlReport

cd ..
start TestResults\HtmlReport\index.html

dotnet build CoverageParser\CoverageParser.csproj -c Release
CoverageParser\bin\Release\CoverageParser.exe