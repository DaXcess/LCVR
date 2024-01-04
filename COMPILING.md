# Compiling

## Initial setup

We recommend using Visual Studio with the .NET development features to compile this project. You can [download Visual Studio here](https://visualstudio.microsoft.com/downloads/). You should use the Community edition as that one is free.

Alternitavely you may also manually install the .NET development tools and compile the project from command line. You will need to [download the .NET SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or higher, since this project is using C# 12.

You can download it from the link above, or alternitavely if you are using Arch Linux you can install dotnet using the following command:

```sh
$ pacman -S dotnet-sdk
```

## Copying the game assembly

This project depends on code from the actual game (what a surprise), but since the game is not free this code has not been provided in this repository by default. If you have installed the game, browse the games local files and copy `Lethal Company_data/Managed/Assembly-CSharp.dll` and `Lethal Company_data/Managed/DissonanceVoip.dll` to the `Libs` directory in the repo. If your IDE was opened during this process, you might have to restart or reload it.

## Building the assembly

Now that the project is set up, you may compile it by either building it using Visual Studio, or by running the following command in the project root:

```sh
$ dotnet build
```

Alternitavely if you're building a finished product run this command:

```sh
$ dotnet build --configuration Release
```

The built plugin assembly can now be found inside the `bin` folder.
