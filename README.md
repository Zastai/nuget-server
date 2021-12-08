# Zastai.NuGet.Server  [![Build Status][CI-S]][CI-L]

This is a greenfield implementation of a NuGet server, including symbol
server functionality.

It will be based solely on the [NuGet Documentation][NuGetDocs], plus
any observed behaviour of client applications.

The initial focus will be on making all the bits and pieces work for
command-line use. That should make it usable for in-house use with CI
or similar. Authentication/Authorization and UI will be added later.

## Release Notes

These are available [on GitHub][GHReleases].

[CI-S]: https://img.shields.io/appveyor/build/zastai/nuget-server
[CI-L]: https://ci.appveyor.com/project/Zastai/nuget-server

[NuGetDocs]: https://docs.microsoft.com/en-us/nuget/api/overview

[GHReleases]: https://github.com/Zastai/nuget-server/releases
