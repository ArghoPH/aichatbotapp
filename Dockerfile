FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["AiChatbotApp.csproj", "./"]
RUN dotnet restore "./AiChatbotApp.csproj"

COPY . .
RUN dotnet publish "./AiChatbotApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AiChatbotApp.dll"]