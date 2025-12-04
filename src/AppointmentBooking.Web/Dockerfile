FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr. microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files (Web no longer needs Infrastructure!)
COPY ["src/AppointmentBooking.Web/AppointmentBooking.Web.csproj", "src/AppointmentBooking.Web/"]
COPY ["src/AppointmentBooking.Application/AppointmentBooking.Application.csproj", "src/AppointmentBooking.Application/"]
COPY ["src/AppointmentBooking. Core/AppointmentBooking. Core.csproj", "src/AppointmentBooking.Core/"]

# Restore dependencies
RUN dotnet restore "src/AppointmentBooking.Web/AppointmentBooking.Web.csproj"

# Copy all source code
COPY . .

# Build
WORKDIR "/src/src/AppointmentBooking.Web"
RUN dotnet build "AppointmentBooking.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AppointmentBooking.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish . 

# Set environment variables (can be overridden by docker-compose)
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "AppointmentBooking.Web.dll"]