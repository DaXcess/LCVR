# LCVRPlugin `Public interface`

## Members
### Methods
#### Public  methods
| Returns | Name |
| --- | --- |
| `void` | [`OnConfigChanged`](#onconfigchanged)()<br>Executed whenever the user changes the configuration for LCVR using the Settings Manager. |
| `void` | [`OnLoad`](#onload)()<br>Executed whenever the LCVR API is loaded. Can be used as an entrypoint to your plugin. |
| `void` | [`OnLobbyJoined`](#onlobbyjoined)()<br>Executed whenever the local player joins a lobby. |
| `void` | [`OnLobbyLeft`](#onlobbyleft)()<br>Executed whenever the local player leaves a lobby. |
| `void` | [`OnLocalPlayerDied`](#onlocalplayerdied)()<br>Executed whenever the local player dies. |
| `void` | [`OnPauseMenuClosed`](#onpausemenuclosed)()<br>Executed whenever the pause menu is closed. This method only gets executed when playing in VR. |
| `void` | [`OnPauseMenuOpened`](#onpausemenuopened)()<br>Executed whenever the pause menu is opened. This method only gets executed when playing in VR. |
| `void` | [`OnVRPlayerDied`](#onvrplayerdied)(`VRNetPlayer` player)<br>Executed whenever a VR player dies. |
| `void` | [`OnVRPlayerJoined`](#onvrplayerjoined)(`VRNetPlayer` player)<br>Executed whenever a VR player has joined the lobby. |
| `void` | [`OnVRPlayerLeft`](#onvrplayerleft)(`VRNetPlayer` player)<br>Executed whenever a VR player has left the lobby. |

## Details
### Methods
#### OnLoad
```csharp
public void OnLoad()
```
##### Summary
Executed whenever the LCVR API is loaded. Can be used as an entrypoint to your plugin.

#### OnConfigChanged
```csharp
public void OnConfigChanged()
```
##### Summary
Executed whenever the user changes the configuration for LCVR using the Settings Manager.

#### OnLobbyJoined
```csharp
public void OnLobbyJoined()
```
##### Summary
Executed whenever the local player joins a lobby.

#### OnLobbyLeft
```csharp
public void OnLobbyLeft()
```
##### Summary
Executed whenever the local player leaves a lobby.

#### OnVRPlayerJoined
```csharp
public void OnVRPlayerJoined(VRNetPlayer player)
```
##### Arguments
| Type | Name | Description |
| --- | --- | --- |
| `VRNetPlayer` | player | The VR player that joined |

##### Summary
Executed whenever a VR player has joined the lobby.

#### OnVRPlayerLeft
```csharp
public void OnVRPlayerLeft(VRNetPlayer player)
```
##### Arguments
| Type | Name | Description |
| --- | --- | --- |
| `VRNetPlayer` | player | The VR player that left |

##### Summary
Executed whenever a VR player has left the lobby.

#### OnLocalPlayerDied
```csharp
public void OnLocalPlayerDied()
```
##### Summary
Executed whenever the local player dies.

#### OnVRPlayerDied
```csharp
public void OnVRPlayerDied(VRNetPlayer player)
```
##### Arguments
| Type | Name | Description |
| --- | --- | --- |
| `VRNetPlayer` | player |   |

##### Summary
Executed whenever a VR player dies.

#### OnPauseMenuOpened
```csharp
public void OnPauseMenuOpened()
```
##### Summary
Executed whenever the pause menu is opened. This method only gets executed when playing in VR.

#### OnPauseMenuClosed
```csharp
public void OnPauseMenuClosed()
```
##### Summary
Executed whenever the pause menu is closed. This method only gets executed when playing in VR.
