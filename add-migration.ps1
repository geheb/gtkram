Param(
    [Parameter(Mandatory=$True)]
    [string]$name
)
Push-Location ./src/GtKram.Core
dotnet ef migrations add $name --startup-project ../GtKram.Ui/
Pop-Location