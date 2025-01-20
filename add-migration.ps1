Param(
    [Parameter(Mandatory=$True)]
    [string]$name
)
Push-Location ./src/GtKram.Infrastructure
dotnet ef migrations add $name --startup-project ../GtKram.Ui/ -o Persistence/Migrations
Pop-Location