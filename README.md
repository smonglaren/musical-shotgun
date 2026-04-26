# Musical Shotgun
<!-- this does not link to the correct video ## <a href="https://youtu.be/5060sr_bdGY"> DEMONSTRATION VIDEO </a> -->
Modifies the shotgun (and all other weapons if desired) to play a sound cue when they're being handled improperly.
Client side only, multiplayer compatible.

## Configuration:
### SoundFilename
*default: **audio.mp3***   
File name of the audio file to play, in the same directory as the mod's .dll file. ONLY .MP3 FILES ARE SUPPORTED
### ShotgunOnly
*default: **true***     
Should this mod affect all weapons, or just the shotgun? If false, this patches all weapons with the ItemGun component, so \*\**IN THEORY*\*\* modded weapons as well.
### AudioStartThreshold
*default: **0.00***    
How far away weapon needs to turned (between 0.00 = facing foward and 1.00 = facing player) for the audio to start playing. To figure out what value you want the range to be, check VOL in additional debug logging.
### AdditionalDebugLogging
*default: **false***   
Enables sending additional messages in the console. These are sent *every frame* a player is holding a weapon. Format is:
`DIFF: {X}, VOL: {Y}, PLAYER: {Z}, PLAYING {U}`

| Variable |  Explanation  
| - | ----
| X | The difference in euler angles between the weapon and player camera.   
| Y | 1 - ( ( X - 180 ) / 180 )  
| Z | The angle of the camera the last player holding the weapon   
| U | True if audio is playing. False if not. 
