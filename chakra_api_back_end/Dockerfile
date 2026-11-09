# 请参阅 https://aka.ms/customizecontainer 以了解如何自定义调试容器，以及 Visual Studio 如何使用此 Dockerfile 生成映像以更快地进行调试。

# 此阶段用于在快速模式(默认为调试配置)下从 VS 运行时
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
USER root
# Alpine 用 apk 安装时区数据
RUN apk add --no-cache tzdata \
    && cp /usr/share/zoneinfo/Asia/Shanghai /etc/localtime \
    && echo "Asia/Shanghai" > /etc/timezone 
ENV TZ=Asia/Shanghai
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# 此阶段用于生成服务项目
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/ApiHost/ApiHost.csproj", "src/ApiHost/"]
COPY ["src/Modules/Content/Content.csproj", "src/Modules/Content/"]
COPY ["src/Modules/Identity/Identity.csproj", "src/Modules/Identity/"]
COPY ["src/Modules/Logging/Logging.csproj", "src/Modules/Logging/"]
COPY ["src/SharedKernel/SharedKernel.csproj", "src/SharedKernel/"]
# BuildKit 缓存挂载：NuGet 包在多次构建间复用，不重复下载
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "./src/ApiHost/ApiHost.csproj"
COPY . .
WORKDIR "/src/src/ApiHost"
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet build "./ApiHost.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 此阶段用于发布要复制到最终阶段的服务项目
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "./ApiHost.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 此阶段在生产中使用，或在常规模式下从 VS 运行时使用(在不使用调试配置时为默认值)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER root
# 预先创建 uploads 目录并将属主收敛给运行用户（仅保留属主读写执行权限）
RUN mkdir -p /app/uploads \
    && chown -R $APP_UID:$APP_UID /app \
    && chmod -R 750 /app/uploads

USER $APP_UID
ENTRYPOINT ["dotnet", "ApiHost.dll"]