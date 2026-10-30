# Multi-stage build for ToDoWebApp
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Proje dosyasını kopyala ve bağımlılıkları yükle
COPY ["ToDoWebApp.csproj", "./"]
RUN dotnet restore "ToDoWebApp.csproj"

# Tüm kaynak kodunu kopyala ve release olarak derle
COPY . .
RUN dotnet publish "ToDoWebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Çalışma zamanı imajı
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

# Güvenlik için non-root kullanıcı ile çalıştır
USER $APP_UID

ENTRYPOINT ["dotnet", "ToDoWebApp.dll"]
