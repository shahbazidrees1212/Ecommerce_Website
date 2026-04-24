FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

WORKDIR /src/Ecommerce_Website
RUN dotnet restore "Ecommerce_Website.csproj"
RUN dotnet publish "Ecommerce_Website.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

CMD ["dotnet", "Ecommerce_Website.dll"]
