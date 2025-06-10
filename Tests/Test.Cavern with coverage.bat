cd Test.Cavern
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=../TestResults/coverage
reportgenerator -reports:../TestResults/coverage.cobertura.xml -targetdir:../TestResults/HtmlReport
start ../TestResults/HtmlReport/index.html