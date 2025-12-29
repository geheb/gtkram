Param(
    [Parameter(Mandatory=$True)]
    [string]$name
)
Push-Location ./src/GtKram.Infrastructure
dotnet ef migrations add $name --startup-project ../../src/GtKram.WebApp/ -o Database/Migrations
Pop-Location