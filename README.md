# Zombie
This is Zombie! An app that never dies, and keeps coming for your release files. It allows you to synch files from GitHub Releases directly to specified locations on the user drive. It runs continously in the background making sure that you are always up to date with your latest GitHub Release. 

<p align="center">
  <img width="500" height="500" src="/_graphics/PNG/iconsZombie.png">
</p>

## What is Zombie?:

1. Zombie Service.
2. Zombie GUI.

### 1. Zombie Service

This part of Zombie runs in the background as a [Windows Service](https://docs.microsoft.com/en-us/dotnet/framework/windows-services/introduction-to-windows-service-applications). The reason it's a Windows Service is simple: we needed it to be a System level application with admin privelages. Since the main job that this service does, is to move files around on a computer we needed it to have admin rights in case that we need to copy some files into ProgramData, ProgramFiles etc. 

ZombieService also hosts a [WCF (Windows Commmunications Foundation)](https://docs.microsoft.com/en-us/dotnet/framework/wcf/whats-wcf) Service inside of it. What is that? It's a server. What for? Well, Windows Service cannot have a GUI component to it, so we needed to de-couple the GUI from the Service. Now that they are seperate and actually two (2) seperate processes, we needed a way to communicate between them, in case that you wanted to ask the Service to do something (perform an update etc.). 

### 2. Zombie GUI

This part of Zombie is a WPF App that allows us to create, and set settings for the Zombie Service. Like I said, Windows Service doesn't have a GUI so this is a stand-alone app, that uses a WCF framework to talk to the service. It uses port 8000 to do the talking. 

## How does Zombie work: 

1. Publish GitHub Release: [How to create a Release](https://help.github.com/articles/creating-releases/)
2. Get Access Token for that Repository: [How to create a Personal Token](https://help.github.com/articles/creating-a-personal-access-token-for-the-command-line/)
3. Setup Zombie GitHub Connection: 

<p align="center">
  <img src="/_info/githubsettings.png">
</p>

4. Setup Zombie File Mappings:

<p align="center">
  <img src="/_info/fileMappings.png">
</p>

5. Setup Zombie Refresh Interval:

<p align="center">
  <img src="/_info/generalSettings.png">
</p>

