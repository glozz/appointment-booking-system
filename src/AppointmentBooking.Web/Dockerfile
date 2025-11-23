FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AppointmentBooking.Web/AppointmentBooking.Web.csproj", "src/AppointmentBooking.Web/"]
COPY ["src/AppointmentBooking.Application/AppointmentBooking.Application.csproj", "src/AppointmentBooking.Application/"]
COPY ["src/AppointmentBooking.Core/AppointmentBooking.Core.csproj", "src/AppointmentBooking.Core/"]
COPY ["src/AppointmentBooking.Infrastructure/AppointmentBooking.Infrastructure.csproj", "src/AppointmentBooking.Infrastructure/"]
RUN dotnet restore "src/AppointmentBooking.Web/AppointmentBooking.Web.csproj"
COPY . .
WORKDIR "/src/src/AppointmentBooking.Web"
RUN dotnet build "AppointmentBooking.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AppointmentBooking.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AppointmentBooking.Web.dll"]
