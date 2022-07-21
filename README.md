# Plough

Plough is a library that can be used to quickly develop back-end F# functionality.

* WebApi: Library to design web api with shared api server/client definition project. Plough.WebApi has compatibility with F# Fable.

## Install dependencies and first build

In order to install the dependencies, build the projects and run the tests just type the following commands:

On windows:
```shell
./fake.cmd build
```

On Unix:
```shell
./fake.sh build
```


## Releasing

### On Windows:

1. Open `release.ps1` and place your [NUGET_TOKEN](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package#create-api-keys=) and [GITHUB_TOKEN](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token).  This file is in `.gitignore` so it should be difficult to push your keys to github.
2. Run `.\release.ps1`

### On Unix

```bash
NUGET_TOKEN=myNugetToken GITHUB_TOKEN=myGithubToken ./fake.sh build -t Release
```
