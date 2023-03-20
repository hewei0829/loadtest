# FROM tiangolo/uwsgi-nginx-flask:python3.6
# RUN pip install redis

# FROM alpine:latest

# ARG JMETER_VERSION="5.3"
# ENV JMETER_HOME /opt/apache-jmeter-${JMETER_VERSION}
# ENV JMETER_BIN  ${JMETER_HOME}/bin
# ENV JMETER_DOWNLOAD_URL https://archive.apache.org/dist/jmeter/binaries/apache-jmeter-${JMETER_VERSION}.tgz

# USER root

# ARG TZ="Europe/Amsterdam"
# RUN    apk update \
#     && apk upgrade \
#     && apk add ca-certificates \
#     && update-ca-certificates \
#     && apk add --update openjdk8-jre tzdata curl unzip bash \
#     && apk add --no-cache nss \
#     && rm -rf /var/cache/apk/ \
#     && mkdir -p /tmp/dependencies  \
#     && curl -L --silent ${JMETER_DOWNLOAD_URL} >  /tmp/dependencies/apache-jmeter-${JMETER_VERSION}.tgz  \
#     && mkdir -p /opt  \
#     && tar -xzf /tmp/dependencies/apache-jmeter-${JMETER_VERSION}.tgz -C /opt  \
#     && rm -rf /tmp/dependencies

# # Set global PATH such that "jmeter" command is found
# ENV PATH $PATH:$JMETER_BIN

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /App
EXPOSE 80

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /App
COPY --from=build-env /App/out .

ENTRYPOINT ["dotnet", "webapi.dll"]

# WORKDIR ${JMETER_HOME}