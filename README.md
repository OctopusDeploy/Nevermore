# ⚠️ Sunset Notice

Octopus Deploy will eventually stop supporting Nevermore. We will archive this git repository and retire the CI/CD infrastructure that supports it.

If you are currently using Nevermore, we recommend you start planning to migrate away from it as soon as reasonably possible.

Please see https://github.com/OctopusDeploy/Nevermore/issues/291 for more information.

# Nevermore

<img src="https://raw.githubusercontent.com/OctopusDeploy/Nevermore/7170eeeca9649b08a56a778e1211f30915bea932/assets/logo%402x.png" width="128" alt="Nevermore logo" />

Nevermore is a micro-ORM that treats SQL Server as a document store. 

> "It's like Dapper, but with JSON"

Nevermore was used in production inside of Octopus Deploy since version 2014, up until version 2024.4.3. 

The Wiki is the best place to get familiar with Nevermore. 

* [Overview](https://github.com/OctopusDeploy/Nevermore/wiki/home)
* [Quickstart](https://github.com/OctopusDeploy/Nevermore/wiki/Quickstart)
* [Why Nevermore?](https://github.com/OctopusDeploy/Nevermore/wiki/Why-Nevermore%3F)

<img alt="Nuget" src="https://img.shields.io/nuget/v/Nevermore?label=NuGet&logo=nuget&style=flat-square">
<img alt="TeamCity" src="https://build.octopushq.com/app/rest/builds/buildType:(id:OctopusDeploy_LIbraries_Nevermore)/statusIcon">

## Integration Tests

Running integration tests requires an MSSQL instance. 

To run integration tests locally using Docker Compose:

```bash
docker compose build --no-cache
docker compose run test
docker compose down
```

We accept [contributions](Contributing.md)!

