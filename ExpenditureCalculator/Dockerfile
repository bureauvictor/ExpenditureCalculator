# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the source code and build the application
COPY . .
RUN dotnet publish -c Release -o /app/out

# Use the ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app/out .

# Expose port 80 for HTTP traffic (if needed)
EXPOSE 5555

# Set the entry point for the container
ENTRYPOINT ["dotnet", "ExpenditureCalculator.dll"]
